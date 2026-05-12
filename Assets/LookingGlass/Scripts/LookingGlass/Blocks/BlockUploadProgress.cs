using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LookingGlass.Blocks {
    public class BlockUploadProgress {
        private UnityWebRequest uploadRequest;
        private string progressText = "";
        private Dictionary<LogType, string> resultsText = new Dictionary<LogType, string>();
        private CreateQuiltHologramArgs createQuiltArgs;
        private HologramData result;
        private Task task;

        public event Action onTextUpdated;

        public UnityWebRequest UploadRequest => uploadRequest;

        public float UploadProgress {
            get {
                if (uploadRequest == null)
                    return 0;
                return uploadRequest.uploadProgress;
            }
        }
        public string ProgressText {
            get { return progressText; }
            private set {
                progressText = value;
                onTextUpdated?.Invoke();
            }
        }

        public CreateQuiltHologramArgs CreateQuiltArgs => createQuiltArgs;
        public HologramData Result => result;

        public string GetResultText(LogType logType) {
            if (resultsText.TryGetValue(logType, out string result))
                return result;
            return null;
        }

        private void PrintResult(LogType logType, string message) {
            if (resultsText.TryGetValue(logType, out string existing))
                resultsText[logType] = existing + message;
            else
                resultsText.Add(logType, message);
        }

        public Task Task => task;

        private BlockUploadProgress() { }

        internal static BlockUploadProgress StartInternal(string filePath, CreateQuiltHologramArgs args, bool useQuiltSuffix = false) {
            BlockUploadProgress progress = new BlockUploadProgress();
            progress.task = LookingGlassUser.UploadFileToBlocksInternal(filePath, args, useQuiltSuffix,
                u => progress.uploadRequest = u,
                t => progress.ProgressText = t,
                progress.PrintResult,
                a => progress.createQuiltArgs = a,
                r => progress.result = r
            );
            return progress;
        }
    }
}