//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LookingGlass.Toolkit;
using static UnityEngine.GraphicsBuffer;
using UnityEditor;

namespace LookingGlass {
    [ExecuteAlways]
    //REVIEW: Do we still have this page?
    // [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/SimpleDOF/")]
    public class SimpleDOF : MonoBehaviour {
        [SerializeField] private HologramCamera camera;

        [Header("DoF Curve")]
        [SerializeField] private float start = -1.5f;
        [SerializeField] private float dip = -0.5f;
        [SerializeField] private float rise = 0.5f;
        [SerializeField] private float end = 2;

        [Header("Blur")]
        [Range(0, 2)]
        [SerializeField] private float blurSize = 1;
        [SerializeField] private bool horizontalOnly = true;
        [SerializeField] private bool testFocus;

        private Material passdepthMaterial;
        private Material boxBlurMaterial;
        private Material finalpassMaterial;

        private void OnEnable() {
            if (!TryEnsureCameraExists())
                return;
            passdepthMaterial = new Material(Shader.Find("LookingGlass/DOF/Pass Depth"));
            boxBlurMaterial = new Material(Shader.Find("LookingGlass/DOF/Box Blur"));
            finalpassMaterial = new Material(Shader.Find("LookingGlass/DOF/Final Pass"));
        }

        private void OnDisable() {
            if (passdepthMaterial != null)
                Material.DestroyImmediate(passdepthMaterial);
            if (boxBlurMaterial != null)
                Material.DestroyImmediate(boxBlurMaterial);
            if (finalpassMaterial != null)
                Material.DestroyImmediate(finalpassMaterial);
        }

        private bool TryEnsureCameraExists() {
            if (camera == null) {
                camera = GetComponentInParent<HologramCamera>();
                if (camera == null) {
                    enabled = false;
                    Debug.LogWarning("[LookingGlass] Simple DOF needs to be on a LookingGlass capture's camera");
                    return false;
                }
            }
            return true;
        }

        public void DoDOF(RenderTexture src, RenderTexture srcDepth) {
            if (!TryEnsureCameraExists())
                return;

            // // make sure the LookingGlass is capturing depth
            // camera.cam.depthTextureMode = DepthTextureMode.Depth;

            Vector4 dofParams = new Vector4(start, dip, rise, end) * camera.CameraProperties.Size;
            dofParams = new Vector4(
                1.0f / (dofParams.x - dofParams.y),
                dofParams.y,
                dofParams.z,
                1.0f / (dofParams.w - dofParams.z)
            );
            boxBlurMaterial.SetVector("dofParams", dofParams);
            boxBlurMaterial.SetFloat("focalLength", camera.CameraProperties.FocalPlane);
            finalpassMaterial.SetInt("testFocus", testFocus ? 1 : 0);
            if (horizontalOnly)
                Shader.EnableKeyword("_HORIZONTAL_ONLY");
            else
                Shader.DisableKeyword("_HORIZONTAL_ONLY");

            RenderTexture fullres = RenderTexture.GetTemporary(src.width, src.height, 0);
            RenderTexture blur1 = RenderTexture.GetTemporary(src.width / 2, src.height / 2, 0);
            RenderTexture blur2 = RenderTexture.GetTemporary(src.width / 3, src.height / 3, 0);
            RenderTexture blur3 = RenderTexture.GetTemporary(src.width / 4, src.height / 4, 0);

            Shader.SetGlobalVector("ProjParams", new Vector4(
                1,
                camera.SingleViewCamera.nearClipPlane,
                camera.SingleViewCamera.farClipPlane,
                1
            ));

            QuiltSettings quiltSettings = camera.QuiltSettings;
            Vector4 tile = new Vector4(
                quiltSettings.columns,
                quiltSettings.rows,
                quiltSettings.tileCount,
                quiltSettings.columns * quiltSettings.rows
            );
            Vector4 viewPortion = new Vector4(
                quiltSettings.ViewPortionHorizontal,
                quiltSettings.ViewPortionVertical
            );
            boxBlurMaterial.SetVector("tile", tile);
            boxBlurMaterial.SetVector("viewPortion", viewPortion);
            finalpassMaterial.SetVector("tile", tile);
            finalpassMaterial.SetVector("viewPortion", viewPortion);

            //Passes: Start with depth
            passdepthMaterial.SetTexture("QuiltDepth", srcDepth);
            Graphics.Blit(src, fullres, passdepthMaterial);

            //Blur 1
            boxBlurMaterial.SetInt("blurPassNum", 0);
            boxBlurMaterial.SetFloat("blurSize", blurSize * 2);
            Graphics.Blit(fullres, blur1, boxBlurMaterial);

            //Blur 2
            boxBlurMaterial.SetInt("blurPassNum", 1);
            boxBlurMaterial.SetFloat("blurSize", blurSize * 3);
            Graphics.Blit(fullres, blur2, boxBlurMaterial);

            //Blur 3
            boxBlurMaterial.SetInt("blurPassNum", 2);
            boxBlurMaterial.SetFloat("blurSize", blurSize * 4);
            Graphics.Blit(fullres, blur3, boxBlurMaterial);

            finalpassMaterial.SetTexture("blur1", blur1);
            finalpassMaterial.SetTexture("blur2", blur2);
            finalpassMaterial.SetTexture("blur3", blur3);

            Graphics.Blit(fullres, src, finalpassMaterial);

            // disposing of stuff
            RenderTexture.ReleaseTemporary(fullres);
            RenderTexture.ReleaseTemporary(blur1);
            RenderTexture.ReleaseTemporary(blur2);
            RenderTexture.ReleaseTemporary(blur3);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SimpleDOF))]
        public class SimpleDOFEditor : Editor {
            private void OnSceneGUI() {
                SimpleDOF dof = (SimpleDOF)target;
                if (dof.camera == null || !dof.enabled) return;

                Transform camTransform = dof.camera.transform;
                float size = dof.camera.CameraProperties.Size;
                Vector3 forward = camTransform.forward;
                Vector3 camPos = camTransform.position;

                Undo.RecordObject(dof, "Adjust DoF Curve");

                // Calculate initial world-space positions
                Vector3 startPos = camPos + forward * (dof.start * size);
                Vector3 dipPos = camPos + forward * (dof.dip * size);
                Vector3 risePos = camPos + forward * (dof.rise * size);
                Vector3 endPos = camPos + forward * (dof.end * size);

                // Draw controllable handles with constraints
                EditorGUI.BeginChangeCheck();
                startPos = DrawSingleAxisHandle(dof, startPos, forward, Color.red, Vector3.negativeInfinity, dipPos);
                dipPos = DrawSingleAxisHandle(dof, dipPos, forward, Color.green, startPos, risePos);
                risePos = DrawSingleAxisHandle(dof, risePos, forward, Color.yellow, dipPos, endPos);
                endPos = DrawSingleAxisHandle(dof, endPos, forward, Color.blue, risePos, Vector3.positiveInfinity);

                if (EditorGUI.EndChangeCheck()) {
                    // Update values based on new positions
                    dof.start = Vector3.Dot(startPos - camPos, forward) / size;
                    dof.dip = Vector3.Dot(dipPos - camPos, forward) / size;
                    dof.rise = Vector3.Dot(risePos - camPos, forward) / size;
                    dof.end = Vector3.Dot(endPos - camPos, forward) / size;

                    EditorUtility.SetDirty(dof);
                }

                // Draw planes for visualization
                DrawPlaneAtPosition(startPos, forward, Color.red);
                DrawPlaneAtPosition(dipPos, forward, Color.green);
                DrawPlaneAtPosition(risePos, forward, Color.yellow);
                DrawPlaneAtPosition(endPos, forward, Color.blue);
            }

            private Vector3 DrawSingleAxisHandle(SimpleDOF dof, Vector3 position, Vector3 forward, Color color, Vector3 minBound, Vector3 maxBound) {
                Handles.color = color;
                Quaternion rotation = Quaternion.LookRotation(forward);
                float handleSize = HandleUtility.GetHandleSize(position) * 0.5f;

                // Draw only the forward axis arrow
                Handles.ArrowHandleCap(0, position, rotation, handleSize, EventType.Repaint);

                // Constrain movement between minBound and maxBound along forward axis
                Vector3 newPos = Handles.Slider(position, forward, handleSize, Handles.ArrowHandleCap, 0.1f);
                float newDist = Vector3.Dot(newPos - dof.camera.transform.position, forward);
                float minDist = Vector3.Dot(minBound - dof.camera.transform.position, forward);
                float maxDist = Vector3.Dot(maxBound - dof.camera.transform.position, forward);

                // Clamp the position to stay within bounds
                newDist = Mathf.Clamp(newDist, minDist, maxDist);
                newPos = dof.camera.transform.position + forward * newDist;

                return newPos;
            }

            private void DrawPlaneAtPosition(Vector3 position, Vector3 normal, Color color) {
                Handles.color = color;
                Vector3 v = Vector3.Cross(normal, Vector3.up).normalized;
                if (v.sqrMagnitude < 0.01f) v = Vector3.Cross(normal, Vector3.right).normalized;
                Vector3 u = Vector3.Cross(v, normal).normalized;

                float scale = 2f;
                Vector3 p0 = position - v * scale - u * scale;
                Vector3 p1 = position + v * scale - u * scale;
                Vector3 p2 = position + v * scale + u * scale;
                Vector3 p3 = position - v * scale + u * scale;

                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p1, p2);
                Handles.DrawLine(p2, p3);
                Handles.DrawLine(p3, p0);
            }
        }
#endif
    }



}
