using System;
using UnityEngine;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LookingGlass {
    [Serializable]
    public class HologramCameraGizmos : PropertyGroup {
        /// <summary>
        /// Purely data that contains an array of points, each represented by a <see cref="Vector3"/>.
        /// </summary>
        [Serializable]
        private struct Polygon {
            public Vector3[] points;

            public bool IsValid => points != null && points.Length > 0;

            /// <summary>
            /// Creates a <see cref="Polygon"/> with a given number of points.
            /// </summary>
            /// <param name="pointCount"></param>
            /// <returns></returns>
            public static Polygon Create(int pointCount) {
                if (pointCount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pointCount), pointCount, "The argument must be greater than zero!");
                
                return new Polygon() {
                    points = new Vector3[pointCount]
                };
            }
        }

        private Polygon focalPointQuad =    Polygon.Create(4);

        private Polygon nearClipQuad =      Polygon.Create(4);
        private Polygon farClipQuad =       Polygon.Create(4);

        public bool DrawHandles {
            get { return hologramCamera.drawHandles; }
            set { hologramCamera.drawHandles = value; }
        }

        public Color FrustumColor {
            get { return hologramCamera.frustumColor; }
            set { hologramCamera.frustumColor = value; }
        }

        public Color MiddlePlaneColor {
            get { return hologramCamera.middlePlaneColor; }
            set { hologramCamera.middlePlaneColor = value; }
        }

        public Color HandleColor {
            get { return hologramCamera.handleColor; }
            set { hologramCamera.handleColor = value; }
        }

        public void DrawGizmos(HologramCamera hologramCamera) {
            UpdateAllGizmosPoints();
            DrawFrustum();
            DrawFocalPlane();

#if UNITY_EDITOR
            //Thanks to https://forum.unity.com/threads/solved-how-to-force-update-in-edit-mode.561436/
            //Ensure continuous Update calls:
            if (!Application.isPlaying) {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
#endif
        }

        private void SetGizmosColor(Color color) {
            Gizmos.color = QualitySettings.activeColorSpace == ColorSpace.Gamma ? color.gamma : color;
        }

        private void UpdateAllGizmosPoints() {
            Camera singleViewCamera = hologramCamera.SingleViewCamera;
            float focalPlane = hologramCamera.CameraProperties.FocalPlane;

            UpdatePointsOnFrustum(focalPointQuad, singleViewCamera, focalPlane);
            switch (hologramCamera.CameraProperties.TransformMode) {
                case TransformMode.Volume: {
                        hologramCamera.CameraProperties.CalculateCameraClipPlanesWithoutDepthiness(out float nearClipPlane, out float farClipPlane);

                        UpdatePointsOnFrustum(nearClipQuad, singleViewCamera, nearClipPlane);
                        UpdatePointsOnFrustum(farClipQuad, singleViewCamera, farClipPlane);
                    }
                    break;
                case TransformMode.Camera: {
                        UpdatePointsOnFrustum(nearClipQuad, singleViewCamera, hologramCamera.CameraProperties.NearClipPlane);
                        UpdatePointsOnFrustum(farClipQuad, singleViewCamera, hologramCamera.CameraProperties.FarClipPlane);
                    }
                    break;
                default:
                    throw new NotSupportedException("Unsupported " + nameof(TransformMode) + ": " + hologramCamera.CameraProperties.TransformMode + "!");
            }
        }

        private void DrawFrustum() {
            SetGizmosColor(FrustumColor);
            Draw(nearClipQuad);
            Draw(farClipQuad);
            DrawBetween(nearClipQuad, farClipQuad);
        }


        /// <summary>
        /// Draws a broken, target-style frame for the focal plane.
        /// </summary>
        private void DrawFocalPlane() {
            SetGizmosColor(MiddlePlaneColor);
            for (int i = 0; i < focalPointQuad.points.Length; i++) {
                Vector3 start = focalPointQuad.points[i];
                Vector3 end = focalPointQuad.points[(i + 1) % focalPointQuad.points.Length];
                Gizmos.DrawLine(start, Vector3.Lerp(start, end, 0.333f));
                Gizmos.DrawLine(end, Vector3.Lerp(end, start, 0.333f));
            }
        }

        /// <summary>
        /// Draws a closed polygon using scene gizmos.
        /// </summary>
        /// <param name="polygon"></param>
        private void Draw(Polygon polygon) {
            Assert.IsTrue(polygon.IsValid);

            int count = polygon.points.Length;
            for (int i = 1; i < count; i++)
                Gizmos.DrawLine(polygon.points[i - 1], polygon.points[i]);
            Gizmos.DrawLine(polygon.points[count - 1], polygon.points[0]);
        }

        /// <summary>
        /// Draws lines between every corresponding point between two polygons, using scene gizmos.
        /// </summary>
        private void DrawBetween(Polygon a, Polygon b) {
            Assert.IsTrue(a.IsValid);
            Assert.IsTrue(b.IsValid);

            int count = a.points.Length;
            Assert.AreEqual(count, b.points.Length);
            for (int i = 0; i < count; i++)
                Gizmos.DrawLine(a.points[i], b.points[i]);
        }

        private void UpdatePointsOnFrustum(Polygon quad, Camera camera, float zDistance) {
            quad.points[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, zDistance));
            quad.points[1] = camera.ViewportToWorldPoint(new Vector3(0, 1, zDistance));
            quad.points[2] = camera.ViewportToWorldPoint(new Vector3(1, 1, zDistance));
            quad.points[3] = camera.ViewportToWorldPoint(new Vector3(1, 0, zDistance));
        }
    }
}
