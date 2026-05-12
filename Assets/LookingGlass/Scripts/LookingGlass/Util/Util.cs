using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LookingGlass {
    internal static class Util {
        private static Texture2D opaqueBlackTexture;

        public static Texture2D OpaqueBlackTexture {
            get {
                if (opaqueBlackTexture == null) {
                    opaqueBlackTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    Color32[] blackPixels = new Color32[16];
                    for (int i = 0; i < blackPixels.Length; i++)
                        blackPixels[i] = new Color32(0, 0, 0, 255);
                    opaqueBlackTexture.SetPixels32(blackPixels);
                    opaqueBlackTexture.Apply();

#if UNITY_EDITOR
                    AssemblyReloadEvents.beforeAssemblyReload += DestroyBlackTexture;
#endif
                }
                return opaqueBlackTexture;
            }
        }

#if UNITY_EDITOR
        private static bool alreadyAttemptedReimport = false;

        private static void DestroyBlackTexture() {
            if (opaqueBlackTexture != null)
                Texture2D.DestroyImmediate(opaqueBlackTexture);
        }
#endif

        //NOTE: This is a more robust replacement for calling Shader.Find(string), that detects when Unity accidentally returns null.
        //This may occur when downgrading the Unity project, and perhaps in other unidentified scenarios.
        //Instead of leaving the developer to be scratching their head why the Lenticular shader is null, we just reimport the Resources folder that contains it and automatically re-reference it.
        //This speeds up our workflow!
        public static Shader FindShader(string name) {
            Shader shader = Shader.Find(name);

#if UNITY_EDITOR
            if (shader == null && !alreadyAttemptedReimport) {
                alreadyAttemptedReimport = true;
                try {
                    string resourcesFolderGuid = "4d49f158eb8fe48a89f792f8dd9c09af";
                    Debug.Log("Forcing reimport of the resources folder (GUID = " + resourcesFolderGuid + ") because " + nameof(Shader) + "." + nameof(Shader.Find) + "(string) was returning null.");
                    string resourcesFolderPath = AssetDatabase.GUIDToAssetPath(resourcesFolderGuid);
                    AssetDatabase.ImportAsset(resourcesFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);

                    shader = Shader.Find(name);
                } catch (Exception e) {
                    Debug.LogException(e);
                    return null;
                }
            }
#endif

            return shader;
        }

        /// <summary>
        /// Copies the <paramref name="source"/> texture to the CPU and encodes it to PNG bytes.<br />
        /// This is the minimum amount of work that requires the Unity main thread.
        /// </summary>
        internal static void EncodeToPNGBytes(RenderTexture source, out Texture2D cpuTexture, out byte[] bytes) {
            cpuTexture = ReadTextureToCPU(source);
            bytes = cpuTexture.EncodeToPNG(); //NOTE: PNG files are typically always encoded in Gamma space, regardless of what color space the Texture2D is in.
        }

        internal static Texture2D SaveAsPNGScreenshotAt(RenderTexture source, string filePath) {
            EncodeToPNGBytes(source, out Texture2D cpuTexture, out byte[] bytes);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);

            Debug.Log("Took screenshot to:    " + filePath + "!");
            return cpuTexture;
        }

        internal static Texture2D ReadTextureToCPU(RenderTexture source) {
            //NOTE: We need to make sure we're creating the Texture2D in the right color space and format, to match the RenderTexture since we're reading pixels directly:
            //RenderTexture in R8G8B8A8_UNorm (Linear)  → Texture2D in RGBA32, bool linear = true
            //RenderTexture in R8G8B8A8_SRGB (Gamma)    → Texture2D in RGBA32, bool linear = false
            //  GraphicsFormats (left) have the "linear vs. gamma" property inherently tied into the enum.
            //  TextureFormats (right) do NOT specify which color space it represents.

            GraphicsFormat graphicsFormat = source.graphicsFormat;
            bool isLinear = !GraphicsFormatUtility.IsSRGBFormat(graphicsFormat);
            TextureFormat format = GraphicsFormatUtility.GetTextureFormat(graphicsFormat);

            Texture2D result = new Texture2D(source.width, source.height, format, false, isLinear);

            try {
                RenderTexture.active = source;
                result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                result.Apply();
            } finally {
                RenderTexture.active = null;
            }

            return result;
        }

        private static StringBuilder sb = null;
        public static string ToString(Matrix4x4 matrix, string indent = null) {
            if (sb == null)
                sb = new StringBuilder(256);
            else
                sb.Clear();

            for (int r = 0; r < 4; r++) {
                if (indent != null)
                    sb.Append(indent);
                for (int c = 0; c < 4; c++) {
                    sb.Append(matrix[r, c].ToString("F8"));
                    if (c < 3)
                        sb.Append(' ');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
