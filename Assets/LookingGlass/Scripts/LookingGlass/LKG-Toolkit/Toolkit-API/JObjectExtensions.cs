#if HAS_NEWTONSOFT_JSON
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LookingGlass.Toolkit {
    public static class JObjectExtensions {
        public static bool TryGet<T>(this JToken @this, string propertyName, out T child) {
            JObject obj = @this.Value<JObject>();
            if (obj == null) {
                child = default;
                return false;
            }

            return TryGet<T>(obj, propertyName, out child);
        }

        public static bool TryGet<T>(this JObject @this, string propertyName, out T child) {
            child = default;
            if (!@this.TryGetValue(propertyName, out JToken value))
                return false;

            bool skipNormalAssignment = false;
            T result = default;

            //LKG Bridge JSON edge case of "{}"...
            if (value.Type == JTokenType.String) {
                string stringValue = value.Value<string>();
                if (string.IsNullOrEmpty(stringValue))
                    return false;

                //NOTE: Special case!
                //  This interprets LKG Bridge's JSON values that are strings containing JSON as just JSON.
                //  Basically, "{}" → {}

                //value.Type is the type of the JSON value,
                //typeof(T) is the type we're trying to get out of this in our C# code,
                //So if it's a JSON string, and we're trying to get out a JObject {}, then:
                if (typeof(T) == typeof(JObject)) {
                    JObject objectInString = JObject.Parse(stringValue);
                    if (objectInString is T converted) {
                        result = converted;
                        skipNormalAssignment = true;
                    }
                }
            }

            if (!skipNormalAssignment)
                result = value.Value<T>();

            if (result == null)
                return false;

            child = result;
            return true;
        }

        public static bool TryGet<T>(this JObject @this, string propertyName, string innerPropertyName, out T child) {
            child = default;
            if (!@this.TryGet(propertyName, out JObject firstChild))
                return false;
            if (!firstChild.TryGet(innerPropertyName, out T result))
                return false;
            child = result;
            return true;
        }

        public static void Add(this JObject @this, string propertyName, string innerPropertyName, object value) {
            @this.Add(
                new JProperty(propertyName,
                    new JObject(
                        new JProperty(innerPropertyName, value)
                    )
                )
            );
        }

        public static string FormatPrettyJSON(this JObject @this, char indentChar, int indentSize) {
            using (StringWriter stringWriter = new())
            using (JsonTextWriter jsonWriter = new(stringWriter) {
                Formatting = Formatting.Indented,
                Indentation = indentSize,
                IndentChar = indentChar
            }) {
                @this.WriteTo(jsonWriter);
                return stringWriter.ToString();
            }
        }
    }
}
#endif
