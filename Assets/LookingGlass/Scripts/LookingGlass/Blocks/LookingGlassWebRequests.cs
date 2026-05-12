using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GraphQLClient;

namespace LookingGlass.Blocks {
    public static class LookingGlassWebRequests {
        [Serializable]
        public struct BlocksAPIServers {
            public string clientId;
            public string authEndpoint;
            public string audience;
            public string endpoint;
            public string graphQLEndpoint;

            public bool IsValid =>
                !string.IsNullOrEmpty(clientId) &&
                !string.IsNullOrEmpty(authEndpoint) &&
                !string.IsNullOrEmpty(audience) &&
                !string.IsNullOrEmpty(endpoint) &&
                !string.IsNullOrEmpty(graphQLEndpoint);
        };

        private static BlocksAPIServers servers;
        private static BlocksAPIServers prod = new BlocksAPIServers {
            clientId = "wsiWbN8u0D9WH6SAzPcGkH3pmyZzqoTt",
            authEndpoint = "https://blocks.us.auth0.com",
            audience = "https://blocks.glass",
            endpoint = "https://blocks.glass",
            graphQLEndpoint = "https://blocks.glass/api/graphql"
        };
        private static BlocksAPIServers staging = new BlocksAPIServers {
            clientId = "txIh2zmKgU8JdCvCORtEa2zPcuSdWrLI",
            authEndpoint = "https://blocks-staging.us.auth0.com",
            audience = "https://staging.blocks.glass",
            endpoint = "https://staging.blocks.glass",
            graphQLEndpoint = "https://staging.blocks.glass/api/graphql"
        };

        private static GraphQLAPI blocksAPI;
        public static GraphQLAPI BlocksAPI {
            get {
                if (blocksAPI == null)
                    blocksAPI = Resources.Load<GraphQLAPI>("Blocks API");
                blocksAPI.SetAuthToken(LookingGlassUser.AccessToken);
                return blocksAPI;
            }
        }

        public static BlocksAPIServers Servers {
            get {
                if (!servers.IsValid)
                    servers = prod;
                return servers;
            }
        }

        private static void ValidateIsLoggedIn() {
            if (!LookingGlassUser.IsLoggedIn)
                throw new MustBeLoggedInException();
        }

        public static string GetViewURL(string userId, int hologramId) => Servers.audience + "/" + userId + "/" + hologramId;
        public static string GetEditURL(string userId, int hologramId) => Servers.audience + "/" + userId + "/" + hologramId + "/edit";

        internal static UnityWebRequest CreateRequestForUserAuthorization(bool permanent = false) {
            BlocksAPIServers servers = Servers;
            WWWForm form = new WWWForm();
            form.AddField("client_id", servers.clientId);
            form.AddField("audience", servers.audience);
            string scope = "openid profile";
            if (permanent) {
                scope = "offline_access " + scope;
            }
            form.AddField("scope", scope);
            return UnityWebRequest.Post(servers.authEndpoint + "/oauth/device/code", form);
        }



        internal static UnityWebRequest CreateRequestToCheckDeviceCode(string deviceCode) {
            BlocksAPIServers servers = Servers;
            WWWForm form = new WWWForm();
            form.AddField("client_id", servers.clientId);
            form.AddField("device_code", deviceCode);
            form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:device_code");
            return UnityWebRequest.Post(servers.authEndpoint + "/oauth/token", form);
        }

        internal static UnityWebRequest CreateRequestToGetFileUploadURL(string fileName) {
            ValidateIsLoggedIn();
            BlocksAPIServers servers = Servers;

            WWWForm form = new WWWForm();
            form.AddField("file", fileName);
            form.AddField("uploadMode", "PUT");

            UnityWebRequest request = UnityWebRequest.Post(servers.endpoint + "/api/upload", form);
            request.SetRequestHeader("Authorization", "Bearer " + LookingGlassUser.AccessToken);

            return request;
        }

        internal static UnityWebRequest CreateRequestToUploadFile(string url, byte[] fileBytes) {
            return UnityWebRequest.Put(url, fileBytes);
        }

        internal static async Task<HologramData> SendRequestToCreateQuiltHologram(CreateQuiltHologramArgs args) {
            if (string.IsNullOrEmpty(args.imageUrl))
                throw new ArgumentException("You must provide a valid image URL!", nameof(args) + "." + nameof(CreateQuiltHologramArgs.imageUrl));
            if (args.fileSize <= 0)
                throw new ArgumentOutOfRangeException("You must provide a file length greater than zero!", nameof(args) + "." + nameof(CreateQuiltHologramArgs.fileSize));

            GraphQLAPI blocksAPI = BlocksAPI;
            string queryName = "CreateQuiltHologram";
            GraphQLQuery query = blocksAPI.GetQueryByName(queryName, GraphQLQueryType.Mutation);
            if (query == null)
                Debug.LogError("Failed to find mutation query by name! (\"" + queryName + "\")");

            query.SetArgs(args);
            return await blocksAPI.Post<HologramData>(query);
        }

        internal static async Task<UserData> SendRequestToGetUserData(string authToken) {
            if (string.IsNullOrEmpty(authToken))
                throw new ArgumentNullException(nameof(authToken));

            GraphQLAPI blocksAPI = BlocksAPI;
            string queryName = "GetUserData";
            GraphQLQuery query = blocksAPI.GetQueryByName(queryName, GraphQLQueryType.Query);
            if (query == null)
                Debug.LogError("Failed to find query by name! (\"" + queryName + "\")");

            blocksAPI.SetAuthToken(authToken);
            return await blocksAPI.Post<UserData>(query);
        }

        internal static async Task SendRequestToDeleteHolograms(IEnumerable<int> ids) {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            int[] array = ids.ToArray();
            if (array.Length <= 0)
                return;

            GraphQLAPI blocksAPI = BlocksAPI;
            string queryName = "DeleteHolograms";
            GraphQLQuery query = blocksAPI.GetQueryByName(queryName, GraphQLQueryType.Mutation);
            if (query == null)
                Debug.LogError("Failed to find query by name! (\"" + queryName + "\")");

            query.SetArgs(new { ids = array });
            await blocksAPI.Post(query);
        }

        internal static UnityWebRequest CreateRequestToLoadUserHolograms() {
            ValidateIsLoggedIn();
            BlocksAPIServers api = Servers;

            //TODO: Switch to a nice GraphQL client like https://github.com/gazuntype/graphQL-client-unity
            string graphQLJson = @"{""query"":""query {myHolograms(first:10) {edges{node {id uuid title}}}}""}";
            string url = api.endpoint + "/api/graphql";
            UnityWebRequest request =
#if UNITY_2022_2_OR_NEWER
                            UnityWebRequest.Post(url, graphQLJson, "application/json");
#else
                            UnityWebRequest.Post(url, graphQLJson);
            request.SetRequestHeader("Content-Type", "application/json");
#endif

            request.SetRequestHeader("Authorization", "Bearer " + LookingGlassUser.AccessToken);
            return request;
        }
    }
}
