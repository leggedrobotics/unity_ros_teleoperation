using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Sensors.Markers
{
    public class LineTest : MonoBehaviour
    {
        public Material lineMaterial;
        public Vector3 startPoint = new Vector3(0, 0, 0);
        public Vector3 endPoint = new Vector3(1, 1, 1);


        void OnRenderObject()
        {
            Debug.Log("OnRenderObject called");
            if (!lineMaterial)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                lineMaterial.SetInt("_ZWrite", 0);
            }
            if (lineMaterial != null)
            {
                lineMaterial.SetPass(0);

                GL.PushMatrix();
                GL.MultMatrix(transform.localToWorldMatrix);

                GL.Begin(GL.LINES);
                GL.Color(Color.red);
                GL.Vertex(startPoint);
                GL.Vertex(endPoint);
                GL.End();
                GL.PopMatrix();
            }
        }
    }
}
