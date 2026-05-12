using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace GraphQLClient {
    [Serializable]
    public class GraphQLQuery {
        [Tooltip("The user-friendly name of this query, displayed in Unity.\n\n" +
            "This is only for UI and finding purposes. It has no effect on the query's functionalities itself.")]
        [FormerlySerializedAs("name")]
        public string displayName;

        public GraphQLQueryType type;

        [FormerlySerializedAs("queryString")]
        [Tooltip("The name of this query from the GraphQL API itself.\n\n" +
            "This must match the GraphQL API in order for the query to be handled properly.")]
        public string name;

        public string returnType;
        public List<string> queryOptions = new List<string>();
        public List<GraphQLField> fields = new List<GraphQLField>();
        public bool isComplete;

        [Tooltip("The final calculated GraphQL query string.")]
        [FormerlySerializedAs("value")]
        [SerializeField] private string query;

        private string args;

        public string DisplayName {
            get { return displayName; }
            internal set { displayName = value; }
        }

        public string Name {
            get { return name; }
            internal set { name = value; }
        }

        public string Query => query;

        public void SetArgs(object inputObject) {
            string json = GraphQLJsonUtility.ToJson(inputObject);
            args = GraphQLJsonUtility.JsonToArguments(json);
            CompleteQuery();
        }

        public void SetArgs(string inputString) {
            args = inputString;
            CompleteQuery();
        }

        public void CompleteQuery() {
            isComplete = true;
            string data = null;
            string parentName = null;
            GraphQLField previousField = null;

            int index = -1;
            foreach (GraphQLField field in fields) {
                index++;
                int parentIndexCount = field.parentIndices.Count;
                if (field.parentIndices.Count == 0) {
                    if (parentName == null) {
                        data += "\n" + GenerateStringTabs(parentIndexCount + 2) + field.name;
                    } else {
                        int diff = previousField.parentIndices.Count - parentIndexCount;
                        while (diff > 0) {
                            data += "\n" + GenerateStringTabs(diff + 1) + "}";
                            diff--;
                        }

                        data += "\n" + GenerateStringTabs(parentIndexCount + 2) + field.name;
                        parentName = null;
                    }

                    previousField = field;
                    continue;
                }

                GraphQLField parent = fields[field.parentIndices.Last()];
                if (parent.name != parentName) {
                    parentName = parent.name;
                    if (parent == previousField) {
                        data += " {\n" + GenerateStringTabs(parentIndexCount + 2) + field.name;
                    } else {
                        int count = previousField.parentIndices.Count - parentIndexCount;
                        while (count > 0) {
                            data += "\n" + GenerateStringTabs(count + 1) + "}";
                            count--;
                        }

                        data += "\n" + GenerateStringTabs(parentIndexCount + 2) + field.name;
                    }

                    previousField = field;

                } else {
                    data += "\n" + GenerateStringTabs(parentIndexCount + 2) + field.name;
                    previousField = field;
                }

                if (index == fields.Count - 1) {
                    int c = previousField.parentIndices.Count;
                    while (c > 0) {
                        data += "\n" + GenerateStringTabs(c + 1) + "}";
                        c--;
                    }
                }

            }

            string arg = String.IsNullOrEmpty(args) ? "" : "(" + args + ")";
            string word;
            switch (type) {
                case GraphQLQueryType.Query:
                    word = "query";
                    break;
                case GraphQLQueryType.Mutation:
                    word = "mutation";
                    break;
                case GraphQLQueryType.Subscription:
                    word = "subscription";
                    break;
                default:
                    word = "query";
                    break;
            }
            query = data == null
                ? word + " " + DisplayName + " {\n" + GenerateStringTabs(1) + Name + arg + "\n}"
                : word + " " + DisplayName + " {\n" + GenerateStringTabs(1) + Name + arg + " {" + data + "\n" + GenerateStringTabs(1) + "}\n}";
        }

        private string GenerateStringTabs(int number) {
            string result = "";
            for (int i = 0; i < number; i++) {
                result += "    ";
            }

            return result;
        }
    }
}
