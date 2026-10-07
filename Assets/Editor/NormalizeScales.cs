using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NormalizeHierarchyScalesSafe
{
    private class TransformState
    {
        public Transform transform;
        public Vector3 worldPosition;
        public Quaternion worldRotation;
    }

    [MenuItem("Tools/Normalize Hierarchy Scales Safe")]
    private static void Normalize()
    {
        Transform root = Selection.activeTransform;

        if (root == null)
        {
            Debug.LogError("Select a root transform.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            root.gameObject,
            "Normalize Hierarchy Scales Safe");

        var states = new List<TransformState>();

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            states.Add(new TransformState
            {
                transform = t,
                worldPosition = t.position,
                worldRotation = t.rotation
            });
        }

        foreach (var state in states)
        {
            if (state.transform == root)
                continue;

            if (state.transform == null)
                continue;

            try
            {
                state.transform.localScale = Vector3.one;
                state.transform.SetPositionAndRotation(
                    state.worldPosition,
                    state.worldRotation);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"Skipped {state.transform.name}: {e.Message}");
            }
        }

        Debug.Log("Hierarchy scales normalized safely.");
    }
}