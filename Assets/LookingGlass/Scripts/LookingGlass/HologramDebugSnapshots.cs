using LookingGlass.Toolkit;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Each hologram debug snapshot contains the following information:
    /// <list type="bullet">
    /// <item>Quilt layer texture</item>
    /// <item>Quilt mix texture (includes the entire render stack)</item>
    /// <item>Render stack</item>
    /// <item>Target LKG display info (includes calibration, default quilt, and hardware info)</item>
    /// <item>Target lenticular region</item>
    /// <item>Lenticular material parameters</item>
    /// </list>
    /// </remarks>
    public static class HologramDebugSnapshots {
        private const string BaseFolder = "Hologram Debugging";
        private static readonly Regex FolderNamePattern = new("Snapshot (?<index>[0-9]+)");
        private static StringBuilder builder;
        private static Task currentTask;

        private static string CombinePaths(params string[] paths) => Path.Combine(paths).Replace('\\', '/');

        public static bool IsBusy() => currentTask != null && !currentTask.IsCompleted;
        public static Task SaveSnapshots() {
            if (IsBusy())
                throw new InvalidOperationException("A debugging snapshot is currently in-progress. Check IsBusy() first before attempting to save a snapshot!\n" +
                    "Only up to one snapshot operation is permitted at any given time.");
            currentTask = SaveSnapshotsInternal();
            return currentTask;
        }

        private static async Task SaveSnapshotsInternal() {
#if UNITY_EDITOR || !UNITY_IOS
            try {
                Debug.Log("Saving hologram debugging snapshots...");

#if HAS_NEWTONSOFT_JSON
                UnityNewtonsoftJSONSerializer.SilentUnityFields = true;
#endif


                string folder = CombinePaths(Environment.CurrentDirectory, BaseFolder);
                Directory.CreateDirectory(folder);
                string[] subfolders = Directory.GetDirectories(folder);
                int maxIndex = -1;
                foreach (string subfolder in subfolders) {
                    string folderName = Path.GetFileName(subfolder);
                    Match m = FolderNamePattern.Match(folderName);
                    if (m.Success) {
                        int index = int.Parse(m.Groups["index"].Value);
                        maxIndex = Mathf.Max(index, maxIndex);
                    }
                }

                int nextIndex = maxIndex + 1;
                string nextFolderName = "Snapshot " + nextIndex;
                string nextFolder = CombinePaths(folder, nextFolderName);
                Directory.CreateDirectory(nextFolder);

                if (builder == null)
                    builder = new StringBuilder(2048);

                foreach (HologramCamera camera in HologramCamera.All) {
                    string name = camera.name;
                    if (camera.TryGetComponent(out QuiltCapture capture)) {
                        string filePath;
                        ScreenshotProgress progress;

                        filePath = CombinePaths(nextFolder, name + " -- Quilt Layer.png");
                        progress = capture.Screenshot3DLayer(filePath, false);
                        await progress.Task;

                        filePath = CombinePaths(nextFolder, name + " -- Quilt Mix.png");
                        progress = capture.Screenshot3D(filePath, false);
                        await progress.Task;

                        builder.Clear();
                        builder.Append(nameof(HologramCamera));
                        builder.Append(": ");
                        builder.AppendLine(name);
                        builder.AppendLine();

                        builder.Append(nameof(LKGSettings));
                        builder.Append(": ");
                        builder.AppendLine(ToJSON(LKGSettingsSystem.Settings));
                        builder.AppendLine();

                        builder.Append(nameof(HologramCamera.RenderStack));
                        builder.Append(": ");
                        builder.AppendLine(ToJSON(camera.RenderStack));
                        builder.AppendLine();

                        builder.Append(nameof(HologramCamera.DisplayInfo));
                        builder.Append(": ");
                        builder.AppendLine(ToJSON(camera.DisplayInfo));
                        builder.AppendLine();

                        builder.Append(nameof(HologramCamera.LenticularRegion));
                        builder.Append(": ");
                        builder.AppendLine(ToJSON(camera.LenticularRegion));
                        builder.AppendLine();


                        int tabSize = 4;
                        builder.AppendLine("Lenticular Material:");
                        Material material = camera.LenticularMaterial;
                        foreach (ShaderPropertyId property in MultiViewRendering.GetAllLenticularProperties()) {
                            builder.Append(' ', tabSize);
                            builder.Append(property.Name);
                            builder.Append(": ");
                            switch (property.Name) {
                                case "pitch":
                                case "slope":
                                case "center":
                                case "subpixelSize":
                                case "screenW":
                                case "screenH":
                                case "tileCount":
                                // --- --- ---
                                case "filterEnd":
                                case "filterSize":
                                // --- --- ---
                                case "gaussianSigma":
                                case "edgeThreshold":
                                    builder.Append(material.GetFloat(property));
                                    break;

                                case "viewPortion":
                                case "tile":
                                // --- --- ---
                                case "aspect":
                                    builder.Append(material.GetVector(property).ToString());
                                    break;

                                case "subpixelCellCount":
                                case "filterMode":
                                case "cellPatternType":
                                case "filterEdge":
                                    builder.Append(material.GetInt(property));
                                    break;

                                case "subpixelCells":
                                    int prevLength = builder.Length;
                                    SubpixelCell[] subpixelCellsInShader = camera.normalizedSubpixelCells;
                                    builder.Append(ToJSON(camera.normalizedSubpixelCells));
                                    if (subpixelCellsInShader != null) {
                                        for (int i = builder.Length - 1; i >= prevLength; i--) {
                                            if (builder[i] == '\n') {
                                                builder.Insert(i + 1, " ", tabSize);
                                            }
                                        }
                                    }
                                    break;
                            }
                            builder.AppendLine();
                        }
                        builder.AppendLine();


                        filePath = CombinePaths(nextFolder, name + " -- Debug Info.txt");
                        string debugText = builder.ToString();
                        await File.WriteAllTextAsync(filePath, debugText);
                    }
                }
                Debug.Log("Finished saving a hologram debugging snapshot.");
            } catch (Exception e) {
                Debug.LogError("An error occurred while trying to save a hologram debugging snapshot!");
                Debug.LogException(e);
            } finally {
#if HAS_NEWTONSOFT_JSON
                UnityNewtonsoftJSONSerializer.SilentUnityFields = false;
#endif
            }
#else
            Debug.LogWarning("Screenshots and recordings are not supported in iOS builds.");
#endif
        }

        private static string ToJSON<T>(T obj) {
#if HAS_NEWTONSOFT_JSON
            return UnityNewtonsoftJSONSerializer.Serialize(obj, true);
#else
            return "";
#endif
        }
    }
}
