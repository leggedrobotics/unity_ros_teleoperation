using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using LookingGlass.Editor;

namespace LookingGlass.Blocks.Editor {
    //TODO: Clean up this script, it's quite messy due to quick prototyping!

    [CustomEditor(typeof(BlockUploader))]
    public class BlockUploaderEditor : UnityEditor.Editor {
        private BlockUploader uploader;

        private Task immediateLogInDelay;
        private Task logInTask;
        private BlockUploadProgress uploadProgress;
        private bool progressBarDone;
        private string userCode;

        private float preferredLabelWidth = -1;

        private bool IsUploading => uploadProgress != null && !uploadProgress.Task.IsCompleted;

        private void OnEnable() {
            uploader = (BlockUploader) target;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            if (preferredLabelWidth < 0) {
                float maxWidth = 0;
                GUIStyle labelStyle = GUI.skin.label;
                SerializedProperty current = serializedObject.GetIterator();
                current.NextVisible(true);
                while (current.NextVisible(false))
                    maxWidth = Mathf.Max(maxWidth, labelStyle.CalcSize(new GUIContent(current.displayName)).x);
                preferredLabelWidth = maxWidth + 25;
            }

            float prevWidth = EditorGUIUtility.labelWidth;
            try {
                EditorGUIUtility.labelWidth = preferredLabelWidth;
                using (new EditorGUI.DisabledScope(!LookingGlassUser.IsLoggedIn || IsUploading)) {
                    SerializedProperty current = serializedObject.GetIterator();
                    current.NextVisible(true);
                    while (current.NextVisible(false)) {
                        if (!CustomGUI(current))
                            EditorGUILayout.PropertyField(current, true);
                    }
                }
            } finally {
                EditorGUIUtility.labelWidth = prevWidth;
            }

            GUILayout.Space(10);
            if (LookingGlassUser.IsLoggedIn) {
                FontStyle prevStyle = GUI.skin.label.fontStyle;
                GUI.skin.label.fontStyle = FontStyle.Italic;
                try {
                    GUILayout.Label("Connected as " + LookingGlassUser.Username + " (" + LookingGlassUser.DisplayName + ")");
                } finally {
                    GUI.skin.label.fontStyle = prevStyle;
                }
                GUILayout.Space(10);
            }
            GUILayout.Space(10);

            BlocksConnectionGUI();
            if (uploadProgress != null && !progressBarDone) {
                Rect r = GUILayoutUtility.GetRect(10, 20, GUILayout.ExpandWidth(true));
                string text = uploadProgress.ProgressText;
                if (text.Contains("Uploading..."))
                    text += " (" + (int) (100 * uploadProgress.UploadProgress) + "%)";

                EditorGUI.ProgressBar(r, uploadProgress.UploadProgress, text);
                Repaint();
            }

            if (uploadProgress != null && uploadProgress.Task.IsCompleted) {
                string errors = uploadProgress.GetResultText(LogType.Error);
                if (errors != null)
                    EditorGUILayout.HelpBox(errors, MessageType.Error);
                else {
                    string warnings = uploadProgress.GetResultText(LogType.Warning);
                    if (warnings != null)
                        EditorGUILayout.HelpBox(warnings, MessageType.Warning);
                    else {
                        string info = uploadProgress.GetResultText(LogType.Log);
                        if (info != null)
                            EditorGUILayout.HelpBox(info, MessageType.Info);
                    }
                }

                GUILayout.Space(10);

                HologramData hologram = uploadProgress.Result;
                if (hologram != null) {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    Color prevColor = GUI.color;
                    try {
                        GUI.color = QuiltCaptureEditor.BlueStartColor;
                        if (GUILayout.Button("  View Block  ", GUILayout.ExpandWidth(false))) {
                                Application.OpenURL(LookingGlassWebRequests.GetViewURL(LookingGlassUser.Username, hologram.id));
                            }
                        // TODO: Need to update Blocks endpoint to handle Hologram edit view
                        // if (hologram.isPublished) {
                        //     if (GUILayout.Button("  View Block  ", GUILayout.ExpandWidth(false))) {
                        //         Application.OpenURL(LookingGlassWebRequests.GetViewURL(LookingGlassUser.Username, hologram.id));
                        //     }
                        // } else {
                        //     if (GUILayout.Button("  Edit Block  ", GUILayout.ExpandWidth(false))) {
                        //         Application.OpenURL(LookingGlassWebRequests.GetEditURL(LookingGlassUser.Username, hologram.id));
                        //     }
                        // }
                    } finally {
                        GUI.color = prevColor;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool CustomGUI(SerializedProperty property) {
            switch (property.name) {
                case nameof(BlockUploader.quiltFilePath):
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(property, new GUIContent(property.displayName, property.tooltip));

                    if (GUILayout.Button(" ... ", GUILayout.ExpandWidth(false))) {
                        string folderPath = "/";
                        string currentQuiltFilePath = property.stringValue;

                        if (!string.IsNullOrWhiteSpace(currentQuiltFilePath))
                            folderPath = Path.GetDirectoryName(currentQuiltFilePath);
                        else if (uploader.TryGetComponent(out QuiltCapture quiltCapture))
                            folderPath = quiltCapture.FolderPath.GetFullPath();

                        if (!Directory.Exists(folderPath))
                            folderPath = "/";

                        string nextValue = EditorUtility.OpenFilePanel("Select Upload File", folderPath, "png,jpeg").Replace('\\', '/').Replace(Application.dataPath.Replace("Assets", "").Trim('/'), "");
                        if (!string.IsNullOrEmpty(nextValue))
                            property.stringValue = nextValue.Trim('/');
                    }
                    GUILayout.EndHorizontal();
                    return true;
            }
            return false;
        }

        private void BlocksConnectionGUI() {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            if (LookingGlassUser.IsLoggedIn)
                LoggedInGUI();
            else
                LoggedOutGUI();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void LoggedOutGUI() {
            EditorGUILayout.HelpBox("You must first connect to Looking Glass Blocks " +
                    "before uploading holograms.", MessageType.Warning);
            GUILayout.Space(10);

            Color prevColor = GUI.color;
            GUI.color = QuiltCaptureEditor.BlueStartColor;
            try {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("    Connect to Blocks    ", GUILayout.ExpandWidth(false))) {
                    logInTask = LookingGlassUser.LogIn((object response) => {
                        switch (response) {
                            case OAuthDeviceCodeResponse deviceCodeMsg:
                                userCode = deviceCodeMsg.user_code;
                                break;
                            case AccessTokenResponse accessTokenMsg:
                                break;
                        }
                    });
                    logInTask.ContinueWith(prevTask => userCode = null);

                    RepaintAfterDone(logInTask);
                    immediateLogInDelay = Task.Delay(1500);
                    RepaintAfterDone(immediateLogInDelay);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            } finally {
                GUI.color = prevColor;
            }

            GUILayout.Space(10);
            if (!string.IsNullOrWhiteSpace(userCode))
                EditorGUILayout.HelpBox("Blocks will open in a new browser window and ask to verify this code: " + userCode, MessageType.Info);
        }

        private void LoggedInGUI() {
            GUILayout.BeginHorizontal();
            Color prevColor = GUI.color;
            GUI.color = QuiltCaptureEditor.BlueStartColor;
            try {
                using (new EditorGUI.DisabledScope(IsUploading)) {
                    if (GUILayout.Button("    Upload    ")) {
                        progressBarDone = false;

                        CreateQuiltHologramArgs args = new CreateQuiltHologramArgs {
                            title = uploader.BlockTitle,
                            description = uploader.BlockDescription
                        };

                        if (uploader.TryGetComponent(out HologramCamera camera))
                            args.CopyFrom(camera.QuiltSettings);

                        uploadProgress = LookingGlassUser.UploadFileToBlocks(uploader.QuiltFilePath, args);
                        uploadProgress.onTextUpdated += Repaint;
                        uploadProgress.Task.ContinueWith(prev => {
                            progressBarDone = true;
                            EditorApplication.delayCall += Repaint;
                        });
                    }
                }
            } finally {
                GUI.color = prevColor;
            }

            GUILayout.Space(5);

            using (new EditorGUI.DisabledScope(IsUploading)) {
                GUI.color = QuiltCaptureEditor.RedRecordingColor;
                try {
                    if (GUILayout.Button("    Log Out    ", GUILayout.ExpandWidth(false))) {
                        uploadProgress = null;
                        LookingGlassUser.LogOut();
                    }
                } finally {
                    GUI.color = prevColor;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void RepaintAfterDone(Task previous) {
            Task logInTask = this.logInTask;

            previous.ContinueWith((Task prev) => {
                if (this != null)
                    EditorApplication.delayCall += Repaint;
            });
        }
    }
}
