using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace GraphQLClient {
    [Serializable]
    public class GraphQLField {
        public int index;
        public int Index {
            get => index;
            set {
                type = possibleFields[value].type;
                name = possibleFields[value].name;
                if (value != index)
                    hasChanged = true;
                index = value;
            }
        }

        public string name;
        public string type;
        [FormerlySerializedAs("parentIndexes")]
        public List<int> parentIndices;
        public bool hasSubField;
        public List<PossibleField> possibleFields;

        public bool hasChanged;

        public GraphQLField() {
            possibleFields = new List<PossibleField>();
            parentIndices = new List<int>();
            index = 0;
        }

        public void CheckSubFields(Introspection.SchemaClass schemaClass) {
            Introspection.SchemaClass.Data.Schema.Type t = schemaClass.data.__schema.types.Find((aType => aType.name == type));
            if (t.fields == null || t.fields.Count == 0) {
                hasSubField = false;
                return;
            }

            hasSubField = true;
        }

        [Serializable]
        public class PossibleField {
            public string name;
            public string type;

            public static implicit operator PossibleField(GraphQLField field) {
                return new PossibleField { name = field.name, type = field.type };
            }
        }
        public static explicit operator GraphQLField(Introspection.SchemaClass.Data.Schema.Type.Field schemaField) {
            Introspection.SchemaClass.Data.Schema.Type ofType = schemaField.type;
            string typeName;
            do {
                typeName = ofType.name;
                ofType = ofType.ofType;
            } while (ofType != null);
            return new GraphQLField { name = schemaField.name, type = typeName };
        }
    }
}
