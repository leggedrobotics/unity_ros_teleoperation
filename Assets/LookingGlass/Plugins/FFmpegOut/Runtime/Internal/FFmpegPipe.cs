//Based on and big thanks to:
//FFmpegOut - FFmpeg video encoding plugin for Unity
//https://github.com/keijiro/KlakNDI

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace FFmpegOut {
    public sealed class FFmpegPipe : IDisposable {
        public static bool IsAvailable => File.Exists(ExecutablePath);
        public static string ExecutablePath {
            get {
                string basePath = Application.streamingAssetsPath;
                RuntimePlatform platform = Application.platform;

                if (platform == RuntimePlatform.OSXPlayer ||
                    platform == RuntimePlatform.OSXEditor)
                    return basePath + "/FFmpegOut/macOS/ffmpeg.app/Contents/MacOS/ffmpeg";

                if (platform == RuntimePlatform.LinuxPlayer ||
                    platform == RuntimePlatform.LinuxEditor)
                    return basePath + "/FFmpegOut/Linux/ffmpeg";

                return basePath + "/FFmpegOut/Windows/ffmpeg.exe";
            }
        }

        private Process subprocess;
        private Thread copyThread;
        private Thread pipeThread;

        private AutoResetEvent copyPing = new AutoResetEvent(false);
        private AutoResetEvent copyPong = new AutoResetEvent(false);
        private AutoResetEvent pipePing = new AutoResetEvent(false);
        private AutoResetEvent pipePong = new AutoResetEvent(false);
        private bool terminate;

        private Queue<NativeArray<byte>> copyQueue = new Queue<NativeArray<byte>>();
        private Queue<byte[]> pipeQueue = new Queue<byte[]>();
        private Queue<byte[]> freeBuffer = new Queue<byte[]>();

        public FFmpegPipe(string arguments) {
            if (!IsAvailable)
                throw new FileNotFoundException("Unable to find the FFmpeg executable!", ExecutablePath);

            subprocess = Process.Start(new ProcessStartInfo {
                FileName = ExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            copyThread = new Thread(CopyThread);
            pipeThread = new Thread(PipeThread);
            copyThread.Start();
            pipeThread.Start();
        }

        ~FFmpegPipe() {
            if (!terminate)
                Debug.LogError(
                    "An unfinalized FFmpegPipe object was detected. " +
                    "It should be explicitly closed or disposed " +
                    "before being garbage-collected."
                );
        }

        public void Dispose() {
            if (!terminate)
                CloseAndGetOutput();
        }

        public void PushFrameData(NativeArray<byte> data) {
            //WARNING: The data parameter may be coming from AsyncGPUReadbackRequest's GetData<T>() method,
            //In which case, we have a race condition against Unity disposing of the NativeArray<T>
            //before our copy thread uses it!

            //TODO: OPTIMIZE?
            //So below, we copy the data into our OWN buffer, whose lifetime we have control over:
            //And NOTE for optimization that most times, the data SHOULD be the same length (width x height)...
            NativeArray<byte> copy = new NativeArray<byte>(data, Allocator.Persistent);

            // Update the copy queue and notify the copy thread with a ping.
            lock (copyQueue)
                copyQueue.Enqueue(copy);
            copyPing.Set();
        }

        public void SyncFrameData() {
            // Wait for the copy queue to get emptied with using pong
            // notification signals sent from the copy thread.
            while (copyQueue.Count > 0)
                copyPong.WaitOne();

            // When using a slower codec (e.g. HEVC, ProRes), frames may be
            // queued too much, and it may end up with an out-of-memory error.
            // To avoid this problem, we wait for pipe queue entries to be
            // comsumed by the pipe thread.
            while (pipeQueue.Count > 4)
                pipePong.WaitOne();
        }

        public string CloseAndGetOutput() {
            terminate = true;

            copyPing.Set();
            pipePing.Set();

            copyThread.Join();
            pipeThread.Join();

            subprocess.StandardInput.Close();
            subprocess.WaitForExit();

            StreamReader outputReader = subprocess.StandardError;
            string error = outputReader.ReadToEnd();

            subprocess.Close();
            subprocess.Dispose();

            outputReader.Close();
            outputReader.Dispose();

            subprocess = null;
            copyThread = null;
            pipeThread = null;
            copyQueue = null;
            pipeQueue = freeBuffer = null;

            return error;
        }

        // CopyThread - Copies frames given from the readback queue to the pipe
        // queue. This is required because readback buffers are not under our
        // control -- they'll be disposed before being processed by us. They
        // have to be buffered by end-of-frame.
        private void CopyThread() {
            while (!terminate) {
                // Wait for ping from the main thread.
                copyPing.WaitOne();

                // Process all entries in the copy queue.
                while (copyQueue.Count > 0) {
                    // Retrieve an copy queue entry without dequeuing it.
                    // (We don't want to notify the main thread at this point.)
                    NativeArray<byte> source;
                    lock (copyQueue)
                        source = copyQueue.Peek();

                    // Try allocating a buffer from the free buffer list.
                    byte[] buffer = null;
                    if (freeBuffer.Count > 0)
                        lock (freeBuffer)
                            buffer = freeBuffer.Dequeue();

                    // Copy the contents of the copy queue entry.
                    if (buffer == null || buffer.Length != source.Length)
                        buffer = source.ToArray();
                    else
                        source.CopyTo(buffer);

                    // Push the buffer entry to the pipe queue.
                    lock (pipeQueue)
                        pipeQueue.Enqueue(buffer);
                    pipePing.Set(); // Ping the pipe thread.

                    // Dequeue the copy buffer entry and ping the main thread.
                    lock (copyQueue)
                        copyQueue.Dequeue();
                    source.Dispose();
                    copyPong.Set();
                }
            }
        }

        // PipeThread - Receives frame entries from the copy thread and push
        // them into the FFmpeg pipe.
        private void PipeThread() {
            Stream pipe = subprocess.StandardInput.BaseStream;

            while (!terminate) {
                // Wait for the ping from the copy thread.
                pipePing.WaitOne();

                // Process all entries in the pipe queue.
                while (pipeQueue.Count > 0) {
                    // Retrieve a frame entry.
                    byte[] buffer;
                    lock (pipeQueue)
                        buffer = pipeQueue.Dequeue();

                    // Write it into the FFmpeg pipe.
                    try {
                        pipe.Write(buffer, 0, buffer.Length);
                        pipe.Flush();
                    } catch {
                        // Pipe.Write could raise an IO exception when ffmpeg
                        // is terminated for some reason. We just ignore this
                        // situation and assume that it will be resolved in the
                        // main thread. #badcode
                    }

                    // Add the buffer to the free buffer list to reuse later.
                    lock (freeBuffer)
                        freeBuffer.Enqueue(buffer);
                    pipePong.Set();
                }
            }
        }
    }
}
