using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLClient;
using UnityEditor;
using UnityEngine;

namespace GraphQLClient.Editor {
    [CustomEditor(typeof(GraphQLAPI))]
    public class GraphAPIEditor : UnityEditor.Editor {
        private GraphQLAPI[] graphs;
        private int index;

        private SerializedProperty urlProperty;
        private GUIStyle titleStyle;
        private GUIStyle queryHeaderStyle;
        private GUIStyle popupStyle;
        private Color greenColor = new Color(0.5f, 1, 0.5f, 1);
        private Color redColor = new Color(1, 0.5f, 0.5f, 1);

        private void OnEnable() {
            graphs = targets.Select(t => (GraphQLAPI) t).ToArray();
            urlProperty = serializedObject.FindProperty(nameof(GraphQLAPI.url));
        }

        public override void OnInspectorGUI() {
            foreach (GraphQLAPI g in graphs) {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(g)))
                    continue;
                g.RegenerateSchemaDataIfNeeded();
            }

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            float shorterLabelWidth = 0.7f * prevLabelWidth;
            EditorGUIUtility.labelWidth = shorterLabelWidth;
            serializedObject.Update();
            try {
                GraphQLAPI graph = (GraphQLAPI) target;
                if (titleStyle == null)
                    titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };

                EditorGUILayout.LabelField(graph.name, titleStyle);
                EditorGUILayout.Space();
                if (GUILayout.Button("Reset")) {
                    Undo.RecordObject(graph, nameof(GraphQLAPI) + " Reset");
                    graph.DeleteAllQueries();
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 0.4f * prevLabelWidth;
                EditorGUILayout.PropertyField(urlProperty);
                EditorGUIUtility.labelWidth = shorterLabelWidth;
                if (GUILayout.Button("  Introspect  ", GUILayout.ExpandWidth(false))) {
                    Undo.RecordObject(graph, "GraphQL Introspect");
                    graph.Introspect();
                }
                EditorGUILayout.EndHorizontal();


                //NOTE: This is when the asset is first being created and named in the Unity editor:
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graph)))
                    return;

                if (graph.IsLoading) {
                    EditorGUILayout.LabelField("API is being introspected. Please wait...");
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                if (graph.SchemaClass == null) {
                    return;
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create New Query")) {
                    Undo.RecordObject(graph, "Create Query");
                    graph.CreateNewQuery();
                }

                if (GUILayout.Button("Create New Mutation")) {
                    Undo.RecordObject(graph, "Create Mutation");
                    graph.CreateNewMutation();
                }

                if (GUILayout.Button("Create New Subscription")) {
                    Undo.RecordObject(graph, "Create Subscription");
                    graph.CreateNewSubscription();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                DisplayFields(graph, graph.Queries, "Query", "Queries");
                DisplayFields(graph, graph.Mutations, "Mutation", "Mutations");
                DisplayFields(graph, graph.Subscriptions, "Subscription", "Subscriptions");
            } finally {
                EditorGUIUtility.labelWidth = prevLabelWidth;
                serializedObject.ApplyModifiedProperties();
            }
        }

        //TODO: Use SerializedProperty / SerializedObject instead!!
        //TODO: Encapsulate the GraphQLQuery class and its fields!
        private void DisplayFields(GraphQLAPI graph, List<GraphQLQuery> queryList, string type, string header) {
            if (queryList == null || queryList.Count <= 0)
                return;

            Color prevColor = GUI.color;

            if (queryHeaderStyle == null)
                queryHeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            EditorGUILayout.LabelField(header, queryHeaderStyle);

            for (int i = 0; i < queryList.Count; i++) {
                EditorGUILayout.Space();

                GraphQLQuery query = queryList[i];
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                query.DisplayName = EditorGUILayout.TextField(type + " Name", query.DisplayName);
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(graph);

                if (query.fields.Count > 0) {
                    if (query.isComplete) {
                        if (GUILayout.Button("Edit", GUILayout.ExpandWidth(false))) {
                            Undo.RecordObject(graph, "Edit Query");
                            graph.EditQuery(query);
                        }
                    } else {
                        if (GUILayout.Button("Preview", GUILayout.ExpandWidth(false))) {
                            Undo.RecordObject(graph, "Preview " + type);
                            query.CompleteQuery();
                        }
                    }
                }

                try {
                    GUI.color = redColor;
                    if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false))) {
                        Undo.RecordObject(graph, "Delete Query");
                        graph.DeleteQuery(queryList, i);
                    }
                } finally {
                    GUI.color = prevColor;
                }
                EditorGUILayout.EndHorizontal();
                string[] options = query.queryOptions.ToArray();

                if (string.IsNullOrEmpty(query.returnType)) {
                    index = EditorGUILayout.Popup(type, index, options);
                    query.Name = options[index];

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Confirm " + type)) {
                        Undo.RecordObject(graph, "Confirm " + type);
                        graph.GetQueryReturnType(query, options[index]);
                    }
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.LabelField(" ", query.Name + "(...): " + query.returnType);

                if (query.isComplete) {
                    GUILayout.Label(query.Query);
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(20));
                try {
                    GUI.color = greenColor;
                    if (graph.CheckSubFields(query.returnType)) {
                        if (GUILayout.Button(new GUIContent("+", "Adds a top-level field to the query."), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))) {
                            Undo.RecordObject(graph, "Add " + type + " Field");
                            graph.GetQueryReturnType(query, options[index]);
                            graph.AddField(query, query.returnType);
                        }
                    }
                } finally {
                    GUI.color = prevColor;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();

                //TODO: I'm not sure if I fully scoped out the Undo for every action here properly
                foreach (GraphQLField field in query.fields) {
                    try {
                        GUI.color = new Color(0.8f, 0.8f, 0.8f);
                        string[] fieldOptions = field.possibleFields.Select(f => f.name).ToArray();

                        EditorGUILayout.BeginHorizontal();
                        if (popupStyle == null)
                            popupStyle = new GUIStyle(EditorStyles.popup);
                        popupStyle.contentOffset = new Vector2(field.parentIndices.Count * 20, 0);

                        EditorGUI.BeginChangeCheck();
                        field.Index = EditorGUILayout.Popup(field.Index, fieldOptions, popupStyle);
                        if (EditorGUI.EndChangeCheck())
                            EditorUtility.SetDirty(graph);
                    } finally {
                        GUI.color = prevColor;
                    }

                    field.CheckSubFields(graph.SchemaClass);
                    if (field.hasSubField) {
                        try {
                            GUI.color = greenColor;
                            if (GUILayout.Button(new GUIContent("+", "Add a new field underneath this one."), GUILayout.ExpandWidth(false))) {
                                Undo.RecordObject(graph, "Add " + type + " Sub-Field");
                                graph.AddField(query, field.possibleFields[field.Index].type, field);
                                break;
                            }
                        } finally {
                            GUI.color = prevColor;
                        }
                    }

                    try {
                        GUI.color = redColor;
                        if (GUILayout.Button("×", GUILayout.MaxWidth(20))) {
                            Undo.RecordObject(graph, "Delete Query");
                            int parentIndex = query.fields.FindIndex(f => f == field);
                            query.fields.RemoveAll(f => f.parentIndices.Contains(parentIndex));
                            query.fields.Remove(field);
                            field.hasChanged = false;
                            break;
                        }
                    } finally {
                        GUI.color = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (field.hasChanged) {
                        int parentIndex = query.fields.FindIndex(f => f == field);
                        query.fields.RemoveAll(f => f.parentIndices.Contains(parentIndex));
                        field.hasChanged = false;
                        break;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.Space();
        }
    }
}

