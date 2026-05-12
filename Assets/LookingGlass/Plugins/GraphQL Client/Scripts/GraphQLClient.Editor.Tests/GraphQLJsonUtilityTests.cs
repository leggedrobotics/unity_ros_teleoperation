using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace GraphQLClient.Editor.Tests {
    public class GraphQLJsonUtilityTests {
        private const string JsonInputMeta = "8fd4c8a3eb3c8f04b88e4b392a1922bb";
        private const string GraphQLOutputMeta = "8fd77351bc71f97459ef8cb51d018ca5";

        private static async Task<string> ReadAllTextAsync(string filePath) {
            //NOTE: We need .NET Standard 2.1+ for File.ReadAllBytesAsync() unfortunately, so we do this instead:
            byte[] fileBytes;
            using (FileStream stream = File.OpenRead(filePath)) {
                fileBytes = new byte[stream.Length];
                await stream.ReadAsync(fileBytes, 0, fileBytes.Length);
            }
            return Encoding.UTF8.GetString(fileBytes);
        }

        [Test]
        public async Task JsonToArgumentsConvertsToCorrectGraphQL() {
            string inputAssetPath = AssetDatabase.GUIDToAssetPath(JsonInputMeta);
            string outputAssetPath = AssetDatabase.GUIDToAssetPath(GraphQLOutputMeta);

            Assert.IsFalse(string.IsNullOrWhiteSpace(inputAssetPath), "Failed to find asset path for JSON input file with GUID: " + JsonInputMeta + "!");
            Assert.IsFalse(string.IsNullOrWhiteSpace(outputAssetPath), "Failed to find asset path for GraphQL output file with GUID: " + GraphQLOutputMeta + "!");

            string json = await ReadAllTextAsync(inputAssetPath);
            string expectedGraphQL = await ReadAllTextAsync(outputAssetPath);
            string actualGraphQLOutput = GraphQLJsonUtility.JsonToArguments(json);

            Debug.Log("INPUT JSON =\n\n" + json);
            Debug.Log("EXPECTED OUTPUT GRAPHQL =\n\n" + expectedGraphQL);
            Debug.Log("ACTUAL OUTPUT GRAPHQL =\n\n" + actualGraphQLOutput);

            Assert.AreEqual(expectedGraphQL, actualGraphQLOutput);
        }
    }
}