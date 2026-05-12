using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
#endif

namespace GraphQLClient {
    public static class GraphQLJsonUtility {
#if HAS_NEWTONSOFT_JSON
        private class EnumInputConverter : StringEnumConverter {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                if (value == null) {
                    writer.WriteNull();
                    return;
                }
                Enum enumValue = (Enum) value;
                string enumText = enumValue.ToString("G");
                writer.WriteRawValue(enumText);
            }
        }

        private static readonly Lazy<EnumInputConverter> EnumConverter = new Lazy<EnumInputConverter>();
#endif

        internal static Exception GetMissingNewtonsoftJsonException() => new NotSupportedException("The Unity package \"com.unity.nuget.newtonsoft-json\" must be installed!");

        public static T FromJson<T>(string json) {
#if HAS_NEWTONSOFT_JSON
            return JsonConvert.DeserializeObject<T>(json);
#else
            throw GetMissingNewtonsoftJsonException();
#endif
        }

        public static string ToJson(object obj) {
#if HAS_NEWTONSOFT_JSON
            return JsonConvert.SerializeObject(obj, EnumConverter.Value);
#else
            throw GetMissingNewtonsoftJsonException();
#endif
        }

        public static string ReformatAsPrettyJson(string json) {
#if HAS_NEWTONSOFT_JSON
            return JObject.Parse(json).ToString(Formatting.Indented);
#else
            throw GetMissingNewtonsoftJsonException();
#endif
        }

        public static string JsonToArguments(string denseJson) {
            //NOTE: For more info on Regex replacement, see https://docs.microsoft.com/en-us/dotnet/standard/base-types/substitutions-in-regular-expressions
            Regex denseJsonFieldNamePattern = new Regex("\"(?<fieldName>[A-Za-z0-9]*)\":");
            string result = denseJsonFieldNamePattern.Replace(denseJson.Trim(), "${fieldName}:");

            //Remove the outermost curly braces:
            result = result.Substring(1, result.Length - 2);

            return result;
        }
    }
}