using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using TMPro;

namespace RSL.Core.TF
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(TFViz))]
    public class TFVizEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TFViz myScript = (TFViz)target;
            if (GUILayout.Button("Visualize TFs"))
            {
                myScript.VizTFs();
            }
        }
    }
    #endif


    public class TFViz : MonoBehaviour
    {
        public GameObject[] tfObjects;
        public List<GameObject> tfTextObjects = new List<GameObject>();
        public GameObject textPrefab;

        public void VizTFs()
        {
            if (tfTextObjects != null && tfTextObjects.Count > 0)
            {
                Debug.Log("Destroying existing text objects");
                // Destroy existing text objects
                for (int i = tfTextObjects.Count - 1; i >= 0; i--)
                {
                    GameObject textObj = tfTextObjects[i];
                    if (textObj != null)
                    {
                        Debug.Log("Destroying text object: " + textObj.name);
                        DestroyImmediate(textObj);
                    }
                }
                tfTextObjects.Clear();

                return; // Exit early if we are just clearing existing objects
            }

            tfObjects = GameObject.FindGameObjectsWithTag("tf");
            foreach (GameObject tf in tfObjects)
            {

                GameObject textObj = Instantiate(textPrefab);
                textObj.transform.localScale = Vector3.one; // Reset scale to default
                textObj.transform.SetParent(tf.transform, false); // Set parent to the TF object
                textObj.transform.localPosition = Vector3.zero;
                textObj.transform.localRotation = Quaternion.identity;
                textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = tf.name;
                if (tf.transform.parent != null)
                {
                    textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text += "\nParent: " + tf.transform.parent.name;
                }

                tfTextObjects.Add(textObj);
            }
        }

        public void Resize(float scale)
        {
            foreach (GameObject textObj in tfTextObjects)
            {
                if (textObj != null)
                {
                    textObj.transform.localScale = new Vector3(scale, scale, scale);
                }
            }
        }
    }
}
