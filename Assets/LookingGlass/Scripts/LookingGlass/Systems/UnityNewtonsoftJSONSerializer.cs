#if HAS_NEWTONSOFT_JSON
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Object = UnityEngine.Object;

namespace LookingGlass {
    public static class UnityNewtonsoftJSONSerializer {
        public static bool SilentUnityFields { get; set; } = false;

        #region Static Section
        //Not expected to work with UnityEditor types as well, since it won't ignore types like Editor, EditorWindow, PropertyDrawer, etc. -- with their fields.
        private class UnityImitatingContractResolver : DefaultContractResolver {
            /// <summary>
            /// Any data types whose fields we don't want to serialize. When any of these types are encountered during serialization,
            /// all of their fields will be skipped.
            /// </summary>
            private static readonly Type[] IgnoreTypes = new Type[] {
                typeof(Object),
                typeof(MonoBehaviour),
                typeof(ScriptableObject)
            };
            private static bool IsIgnoredType(Type type) => Array.FindIndex(IgnoreTypes, (Type current) => current == type) >= 0;

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
                List<FieldInfo> allFields = new List<FieldInfo>();
                Type unityObjType = typeof(Object);

                for (Type t = type; t != null && !IsIgnoredType(t); t = t.BaseType) {
                    FieldInfo[] currentTypeFields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < currentTypeFields.Length; i++) {
                        FieldInfo field = currentTypeFields[i];
                        if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                            continue;
                        if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                            continue;
                        if (unityObjType.IsAssignableFrom(field.FieldType)) {
                            if (!SilentUnityFields)
                                Debug.LogError("Failed to serialize a Unity object reference -- this is not supported by the " + GetType().Name
                                    + ". Ignoring the property. (" + type.Name + "'s \"" + field.Name + "\" field)");
                            continue;
                        }
                        allFields.Add(field);
                    }
                }
                //This sorts them based on the order they were actually written in the source code.
                //Beats me why Reflection wouldn't list them in that order to begin with, but whatever, this works
                allFields.Sort((a, b) => a.MetadataToken - b.MetadataToken);

                List<JsonProperty> properties = new List<JsonProperty>(allFields.Count);
                for (int i = 0; i < allFields.Count; i++) {
                    int index = properties.FindIndex((JsonProperty current) => current.UnderlyingName == allFields[i].Name);
                    if (index >= 0)
                        continue;
                    JsonProperty property = CreateProperty(allFields[i], memberSerialization);
                    property.Writable = true;
                    property.Readable = true;
                    properties.Add(property);
                }
                return properties;
            }
        }

        private class FloatConverter : JsonConverter<float> {
            private int decimalPlaces;
            private string format;

            public int DecimalPlaces {
                get { return decimalPlaces; }
                set {
                    decimalPlaces = Mathf.Min(8, value);
                    format = (decimalPlaces >= 0) ? "F" + decimalPlaces : null;
                }
            }

            public FloatConverter(int decimalPlaces) {
                DecimalPlaces = decimalPlaces;
            }

            public override void WriteJson(JsonWriter writer, float value, JsonSerializer serializer) {
                string stringValue = (format == null) ? value.ToString() : value.ToString(format);
                writer.WriteValue(float.Parse(stringValue));
            }

            public override float ReadJson(JsonReader reader, Type objectType, float existingValue, bool hasExistingValue, JsonSerializer serializer) {
                //For some reason, reader.Value is giving back a double and casting to a float did not go so well, from object to float.
                //And I didn't want to hard code 2 consecutive casts, literally, like "(float) (double) reader.Value", so I'm glad this works:
                return Convert.ToSingle(reader.Value);
            }
        }

        private class GuidConverter : JsonConverter<Guid> {
            public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer) {
                writer.WriteValue(value.ToString().ToUpper());
            }

            public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer) {
                if (reader.Value is string text && Guid.TryParse(text, out Guid result))
                    return result;
                return existingValue;
            }
        }
        #endregion

        private static JsonSerializerSettings settings = new JsonSerializerSettings() {
            ContractResolver = new UnityImitatingContractResolver(),
            Converters = new JsonConverter[] {
                new FloatConverter(-1),
                new GuidConverter(),
                new StringEnumConverter()
            },
            NullValueHandling = NullValueHandling.Ignore,
        };

        private static JsonSerializer lowerLevelSerializer = null;

        private static bool CreateSerializerIfNeeded(ref JsonSerializer serializer) {
            if (serializer == null) {
                serializer = JsonSerializer.Create(settings);
                return true;
            }
            return false;
        }

        //Not used currently, but really cool naming JObject custom converter: https://stackoverflow.com/questions/40244395/how-to-serialize-a-jobject-the-same-way-as-an-object-with-json-net

        public static string Serialize<T>(T obj) => Serialize<T>(obj, false);
        public static string Serialize<T>(T obj, bool prettyPrint) {
            string text;
            Formatting formatting = prettyPrint ? Formatting.Indented : Formatting.None;
            settings.Formatting = formatting;

            CreateSerializerIfNeeded(ref lowerLevelSerializer);
            try {
                //For now, as I am unsure how I want to move forward with looping references, I'll just have it try first -- if it comes up, show an error
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Error;

                //text = JsonConvert.SerializeObject(obj, settings);
                using (StringWriter stringWriter = new())
                using (JsonTextWriter writer = new(stringWriter)) {
                    writer.Indentation = 4;
                    writer.IndentChar = ' ';
                    lowerLevelSerializer.Serialize(writer, obj);
                    text = stringWriter.ToString();
                }
            } catch (JsonSerializationException e) {
                //and then go to these statements and ignore any looping references. This way, it lets us know if it DOES come across looping references.
                //Which aren't supported as this code is written currently.
                Debug.LogException(e);
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                text = JsonConvert.SerializeObject(obj, settings);
            }
            return text;
        }

        public static T Deserialize<T>(string text) {
            try {
                T obj = JsonConvert.DeserializeObject<T>(text, settings);
                return obj;
            } catch (Exception e) {
                Debug.LogError("Failed to deserialize text = " + text + "\n(Length = " + text.Length + ")");
                Debug.LogException(e);
                throw e;
            }
        }

        public static object Deserialize(string text, Type type) {
            try {
                object obj = JsonConvert.DeserializeObject(text, type, settings);
                return obj;
            } catch (Exception e) {
                Debug.LogError("Failed to deserialize text = " + text + "\n(Length = " + text.Length + ") to data type: " + type.FullName);
                Debug.LogException(e);
                throw e;
            }
        }

        public static JObject SerializeToJObject<T>(T source) {
            if (!typeof(T).IsValueType && source == null)
                throw new ArgumentNullException(nameof(source));

            CreateSerializerIfNeeded(ref lowerLevelSerializer);
            return JObject.FromObject(source, lowerLevelSerializer);
        }

        public static void DeserializeFromJObject<T>(JObject json, T targetObject) {
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            if (!typeof(T).IsValueType && targetObject == null)
                throw new ArgumentNullException(nameof(targetObject));

            //This only supports populating from string json text:
            //JsonConvert.PopulateObject(...)

            using (JsonReader reader = json.CreateReader()) {
                CreateSerializerIfNeeded(ref lowerLevelSerializer);
                lowerLevelSerializer.Populate(reader, targetObject);
            }
        }
    }
}
#endif
