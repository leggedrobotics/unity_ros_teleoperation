//Based on and big thanks to:
//FFmpegOut - FFmpeg video encoding plugin for Unity
//https://github.com/keijiro/KlakNDI

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace FFmpegOut {
    public sealed class FFmpegSession : IDisposable {
        private static readonly Lazy<StringBuilder> SharedBuilder = new Lazy<StringBuilder>(() => new StringBuilder(256));
        private static readonly Lazy<HashSet<string>> HashSetBuffer = new Lazy<HashSet<string>>();

        public static FFmpegSession Create(string name, int width, int height, float frameRate, FFmpegPreset preset, string extraFfmpegOptions = "", IEnumerable<MediaMetadataPair> metadata = null) {
            name += DateTime.Now.ToString(" yyyy MMdd HHmmss").Replace(" ", "_");
            string outputFilePath = name + preset.GetFileExtension();
            return CreateWithOutputPath(outputFilePath, width, height, frameRate, preset, extraFfmpegOptions);
        }

        public static FFmpegSession CreateWithOutputPath(string outputFilePath, int width,int height, float frameRate, FFmpegPreset preset, string extraOptions = "", IEnumerable<MediaMetadataPair> metadata = null) {
            string arguments = "-fflags +discardcorrupt " +
                "-y -f rawvideo -vcodec rawvideo -pixel_format rgba"
                + " -video_size " + width + "x" + height
                + " -framerate " + frameRate
                + " -loglevel warning -i - " + preset.GetOptions()
                + " " + extraOptions;

            //NOTE: Thanks to https://github.com/amiaopensource/An_Archivists_Guide_To_Matroska/blob/master/metadata.md#adding-tags-with-ffmpeg
            //For providing details on how to add metadata through FFmpeg-created media files!
            if (metadata != null) {
                lock (SharedBuilder) {
                    StringBuilder sb = SharedBuilder.Value;
                    HashSet<string> alreadyAdded = HashSetBuffer.Value;

                    sb.Clear();
                    sb.Append(arguments);
                    foreach (MediaMetadataPair pair in metadata) {
                        if (pair.key.IndexOf('"') >= 0 || pair.value.IndexOf('"') >= 0) {
                            Debug.LogWarning("Metadata contained double-quotes! Ignoring, since these won't be handled properly by the command-line args...");
                            continue;
                        }
                        if (alreadyAdded.Contains(pair.key)) {
                            Debug.LogWarning("Already added metadata with key name: \"" + pair.key + "\" -- skipping!");
                            continue;
                        }
                        sb.Append(" -metadata \"" + pair.key + "\"=\"" + pair.value + "\"");
                        alreadyAdded.Add(pair.key);
                    }
                    sb.Append(" \"" + outputFilePath + "\"");
                    arguments = sb.ToString();
                }
            } else {
                arguments += " \"" + outputFilePath + "\"";
            }

            FFmpegSession session = new FFmpegSession(
                outputFilePath,
                arguments
            );
            return session;
        }

        private string outputFilePath;
        private FFmpegPipe pipe;
        private Material blitMaterial;
        private List<AsyncGPUReadbackRequest> readbackQueue = new List<AsyncGPUReadbackRequest>(4);

        public string OutputFilePath => outputFilePath;

        FFmpegSession(string outputFilePath, string arguments) {
            this.outputFilePath = outputFilePath;

            if (!FFmpegPipe.IsAvailable)
                Debug.LogWarning(
                    "Failed to initialize an FFmpeg session due to missing " +
                    "executable file. Please check FFmpeg installation."
                );
            else if (!SystemInfo.supportsAsyncGPUReadback)
                Debug.LogWarning(
                    "Failed to initialize an FFmpeg session due to lack of " +
                    "async GPU readback support. Please try changing " +
                    "graphics API to readback-enabled one."
                );
            else {
                Debug.Log("ffmpeg " + arguments);
                pipe = new FFmpegPipe(arguments);
            }
        }

        ~FFmpegSession() {
            if (pipe != null)
                Debug.LogError(
                    "An unfinalized FFmpegCapture object was detected. " +
                    "It should be explicitly closed or disposed " +
                    "before being garbage-collected."
                );
        }

        public void Dispose() {
            Close();
        }

        public void PushFrame(Texture source) {
            if (pipe != null) {
                ProcessQueue();
                if (source != null) QueueFrame(source);
            }
        }

        public void CompletePushFrames() {
            pipe?.SyncFrameData();
        }

        public void Close() {
            if (pipe != null) {
                string error = pipe.CloseAndGetOutput();

                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning(
                        "FFmpeg returned with warning/error messages. " +
                        "See the following lines for details:\n" + error
                    );

                pipe.Dispose();
                pipe = null;
            }

            if (blitMaterial != null) {
                Object.Destroy(blitMaterial);
                blitMaterial = null;
            }
        }

        private void QueueFrame(Texture source) {
            if (readbackQueue.Count > 6) {
                Debug.LogWarning("Too many GPU readback requests.");
                return;
            }

            if (blitMaterial == null) {
                Shader shader = Shader.Find("Hidden/FFmpegOut/Preprocess");
                blitMaterial = new Material(shader);
            }

            // Blit to a temporary texture and request readback on it.
            RenderTexture tex = RenderTexture.GetTemporary
                (source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, tex, blitMaterial, 0);
            readbackQueue.Add(AsyncGPUReadback.Request(tex));
            RenderTexture.ReleaseTemporary(tex);
        }

        private void ProcessQueue() {
            while (readbackQueue.Count > 0) {
                // Check if the first entry in the queue is completed.
                if (!readbackQueue[0].done) {
                    // Detect out-of-order case (the second entry in the queue
                    // is completed before the first entry).
                    if (readbackQueue.Count > 1 && readbackQueue[1].done) {
                        // We can't allow the out-of-order case, so force it to
                        // be completed now.
                        readbackQueue[0].WaitForCompletion();
                    } else {
                        // Nothing to do with the queue.
                        break;
                    }
                }

                AsyncGPUReadbackRequest request = readbackQueue[0];
                readbackQueue.RemoveAt(0);

                // Error detection
                if (request.hasError) {
                    Debug.LogWarning("GPU readback error was detected.");
                    continue;
                }

                // Feed the frame to the FFmpeg pipe.
                pipe.PushFrameData(request.GetData<byte>());
            }
        }
    }
}
