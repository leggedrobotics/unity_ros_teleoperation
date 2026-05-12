using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GraphQLClient {
    [CreateAssetMenu(menuName = "GraphQLClient/GraphQL API", fileName = "GraphQL API")]
    public class GraphQLAPI : ScriptableObject {
        [SerializeField] internal string url;
        [SerializeField] private List<GraphQLQuery> queries         = new List<GraphQLQuery>();
        [SerializeField] private List<GraphQLQuery> mutations       = new List<GraphQLQuery>();
        [SerializeField] private List<GraphQLQuery> subscriptions   = new List<GraphQLQuery>();

        [NonSerialized] private Introspection.SchemaClass schemaClass;

        private bool isLoading;
        private string introspection;
        private string authToken;

        private string queryEndpoint;
        private string mutationEndpoint;
        private string subscriptionEndpoint;

        private UnityWebRequest request;

        public string URL => url;
        public List<GraphQLQuery> Queries => queries;
        public List<GraphQLQuery> Mutations => mutations;
        public List<GraphQLQuery> Subscriptions => subscriptions;
        public Introspection.SchemaClass SchemaClass {
            get {
#if UNITY_EDITOR
                RegenerateSchemaDataIfNeeded();
#endif
                return schemaClass;
            }
        }

        public bool IsLoading => isLoading;

#if UNITY_EDITOR
        /// <summary>
        /// <remarks>NOTE: This is only available in the Unity editor.</remarks>
        /// </summary>
        public string SchemaJsonFilePath => Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)), name + " Schema.json").Replace('\\', '/');
#endif

        public void SetAuthToken(string auth) {
            authToken = auth;
        }

        public GraphQLQuery GetQueryByName(string queryName, GraphQLQueryType type) {
            List<GraphQLQuery> querySearch;
            switch (type) {
                case GraphQLQueryType.Mutation:
                    querySearch = mutations;
                    break;
                case GraphQLQueryType.Query:
                    querySearch = queries;
                    break;
                case GraphQLQueryType.Subscription:
                    querySearch = subscriptions;
                    break;
                default:
                    querySearch = queries;
                    break;
            }
            return querySearch.Find(q => q.DisplayName == queryName);
        }

        public async Task<UnityWebRequest> Post(GraphQLQuery query) => await Post<UnityWebRequest>(query);
        public async Task<T> Post<T>(GraphQLQuery query) {
            if (string.IsNullOrEmpty(query.Query))
                query.CompleteQuery();
            return await UnityWebRequestExtensions.PostAsync<T>(url, query.Name, query.Query, authToken);
        }

        public async Task<ClientWebSocket> Subscribe(GraphQLQuery query, string socketId = "1", string protocol = "graphql-ws") {
            if (string.IsNullOrEmpty(query.Query))
                query.CompleteQuery();
            return await GraphQLHttpHandler.WebsocketConnect(url, query.Query, authToken, socketId, protocol);
        }

        public async Task<ClientWebSocket> Subscribe(string queryName, GraphQLQueryType type, string socketId = "1", string protocol = "graphql-ws") =>
            await Subscribe(GetQueryByName(queryName, type), socketId, protocol);

        public async Task CancelSubscription(ClientWebSocket cws, string socketId = "1") =>
            await GraphQLHttpHandler.WebsocketDisconnect(cws, socketId);

#if UNITY_EDITOR
        #region Editor Use
        //TODO: Put schema file in proper location
        public async Task Introspect() {
            isLoading = true;
            try {
                request = await UnityWebRequestExtensions.PostAsync(url, "", Introspection.schemaIntrospectionQuery, authToken);
                introspection = request.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(introspection)) {
                    Debug.LogError("The downloaded text did not contain JSON containing the schema information.\nAre you sure you have the correct API URL?\nFor example, you can try: https://blocks.glass/api/graphql");
                    return;
                }

                introspection = GraphQLJsonUtility.ReformatAsPrettyJson(introspection);
                string filePath = SchemaJsonFilePath;
                Debug.Log("Wrote schema JSON data to:        " + filePath);
                File.WriteAllText(filePath, introspection);
                RegenerateSchemaData();
                AssetDatabase.Refresh();
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                isLoading = false;
            }
        }

        internal bool RegenerateSchemaDataIfNeeded() {
            if (schemaClass == null) {
                RegenerateFromLocalFile();
                return true;
            }
            return false;
        }

        private bool RegenerateFromLocalFile() {
            string filePath = SchemaJsonFilePath;
            if (File.Exists(filePath)) {
                introspection = File.ReadAllText(filePath);
                RegenerateSchemaData();
                return true;
            }
            return false;
        }

        private void RegenerateSchemaData() {
            schemaClass = GraphQLJsonUtility.FromJson<Introspection.SchemaClass>(introspection);

            if (schemaClass.data.__schema.queryType != null)
                queryEndpoint = schemaClass.data.__schema.queryType.name;
            if (schemaClass.data.__schema.mutationType != null)
                mutationEndpoint = schemaClass.data.__schema.mutationType.name;
            if (schemaClass.data.__schema.subscriptionType != null)
                subscriptionEndpoint = schemaClass.data.__schema.subscriptionType.name;
        }

        public GraphQLQuery CreateNewQuery() {
            GraphQLQuery query = new GraphQLQuery { type = GraphQLQueryType.Query };

            Introspection.SchemaClass.Data.Schema.Type queryType = schemaClass.data.__schema.types.Find(t => t.name == queryEndpoint);
            for (int i = 0; i < queryType.fields.Count; i++)
                query.queryOptions.Add(queryType.fields[i].name);

            queries.Add(query);
            return query;
        }

        public GraphQLQuery CreateNewMutation() {
            GraphQLQuery mutation = new GraphQLQuery { type = GraphQLQueryType.Mutation };

            Introspection.SchemaClass.Data.Schema.Type mutationType = schemaClass.data.__schema.types.Find(t => t.name == mutationEndpoint);
            if (mutationType == null) {
                Debug.Log("Unable to create new mutation since there were no mutation types found.");
                return null;
            }
            for (int i = 0; i < mutationType.fields.Count; i++) {
                mutation.queryOptions.Add(mutationType.fields[i].name);
            }
            mutations.Add(mutation);
            return mutation;
        }

        public GraphQLQuery CreateNewSubscription() {
            GraphQLQuery subscription = new GraphQLQuery { type = GraphQLQueryType.Subscription };

            Introspection.SchemaClass.Data.Schema.Type subscriptionType = schemaClass.data.__schema.types.Find(t => t.name == subscriptionEndpoint);
            if (subscriptionType == null) {
                Debug.LogWarning("Unable to create new subscription since there were no subscription types found.");
                return null;
            }
            for (int i = 0; i < subscriptionType.fields.Count; i++) {
                subscription.queryOptions.Add(subscriptionType.fields[i].name);
            }
            subscriptions.Add(subscription);
            return subscription;
        }

        public void EditQuery(GraphQLQuery query) {
            query.isComplete = false;
        }

        public bool CheckSubFields(string typeName) {
            Introspection.SchemaClass.Data.Schema.Type type = schemaClass.data.__schema.types.Find((t => t.name == typeName));
            if (type?.fields == null || type.fields.Count == 0) {
                return false;
            }
            return true;
        }

        //TODO: Do not allow addition of subfield that already exists
        public void AddField(GraphQLQuery query, string typeName, GraphQLField parent = null) {
            Introspection.SchemaClass.Data.Schema.Type type = schemaClass.data.__schema.types.Find((t => t.name == typeName));
            List<Introspection.SchemaClass.Data.Schema.Type.Field> subFields = type.fields;
            int parentIndex = query.fields.FindIndex(f => f == parent);
            List<int> parentIndexes = new List<int>();
            if (parent != null)
                parentIndexes = new List<int>(parent.parentIndices) { parentIndex };

            GraphQLField fielder = new GraphQLField { parentIndices = parentIndexes };
            foreach (Introspection.SchemaClass.Data.Schema.Type.Field field in subFields)
                fielder.possibleFields.Add((GraphQLField) field);

            if (fielder.parentIndices.Count == 0) {
                query.fields.Add(fielder);
            } else {
                int index;
                index = query.fields.FindLastIndex(f =>
                    f.parentIndices.Count > fielder.parentIndices.Count &&
                    f.parentIndices.Contains(fielder.parentIndices.Last()));

                if (index == -1) {
                    index = query.fields.FindLastIndex(f =>
                        f.parentIndices.Count > fielder.parentIndices.Count &&
                        f.parentIndices.Last() == fielder.parentIndices.Last());
                }

                if (index == -1) {
                    index = fielder.parentIndices.Last();
                }

                index++;
                query.fields[parentIndex].hasChanged = false;
                query.fields.Insert(index, fielder);
            }
        }

        private string GetFieldType(Introspection.SchemaClass.Data.Schema.Type.Field field) {
            GraphQLField newField = (GraphQLField) field;
            return newField.type;
        }

        public void GetQueryReturnType(GraphQLQuery query, string queryName) {
            string endpoint;
            switch (query.type) {
                case GraphQLQueryType.Query:
                    endpoint = queryEndpoint;
                    break;
                case GraphQLQueryType.Mutation:
                    endpoint = mutationEndpoint;
                    break;
                case GraphQLQueryType.Subscription:
                    endpoint = subscriptionEndpoint;
                    break;
                default:
                    endpoint = queryEndpoint;
                    break;
            }
            Introspection.SchemaClass.Data.Schema.Type queryType =
                schemaClass.data.__schema.types.Find((t => t.name == endpoint));
            Introspection.SchemaClass.Data.Schema.Type.Field field =
                queryType.fields.Find((f => f.name == queryName));

            query.returnType = GetFieldType(field);
        }

        public void DeleteQuery(List<GraphQLQuery> query, int index) {
            query.RemoveAt(index);
        }

        public void DeleteAllQueries() {
            queries = new List<GraphQLQuery>();
            mutations = new List<GraphQLQuery>();
            subscriptions = new List<GraphQLQuery>();
        }
        #endregion
#endif
    }
}
