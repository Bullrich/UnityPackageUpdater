using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace PackageUpdater.JsonWrapper
{
    internal class JsonNetParser
    {
        private EndOfStreamException IncompleteJsonException =>
            new EndOfStreamException("Json shouldn't have finished here!");

        public string Serialize(IDictionary<string, object> obj)
        {
            return Serialize((object) obj);
        }

        public string Serialize(object obj)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                SerializeValue(writer, obj);
            }

            return sb.ToString();
        }

        public JsonDictionary Parse(string json)
        {
            return Parse<JsonDictionary>(json);
        }

        public T Parse<T>(string json)
        {
            return (T) ReadJson(json);
        }

        #region Serialization

        private void SerializeValue(JsonTextWriter writer, object value)
        {
            if (value is IDictionary)
                SerializeDictionary(writer, (IDictionary) value);
            else if (value is IList)
                SerializeList(writer, (IList) value);
            else if (value == null)
                writer.WriteNull();
            else
                writer.WriteValue(value);
        }

        private void SerializeDictionary(JsonTextWriter writer, IDictionary dict)
        {
            writer.WriteStartObject();
            foreach (DictionaryEntry kv in dict)
            {
                writer.WritePropertyName((string) kv.Key);
                SerializeValue(writer, kv.Value);
            }

            writer.WriteEndObject();
        }

        private void SerializeList(JsonTextWriter writer, IList list)
        {
            writer.WriteStartArray();
            foreach (var element in list)
                SerializeValue(writer, element);
            writer.WriteEndArray();
        }

        #endregion

        #region Deserialization

        private object ReadJson(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)))
                while (reader.Read())
                {
                    var tokenType = reader.TokenType;
                    switch (tokenType)
                    {
                        case JsonToken.StartObject:
                            return ParseObject(reader);
                        case JsonToken.StartArray:
                            return ParseArray(reader);
                        default:
                            return reader.Value;
                    }
                }

            throw IncompleteJsonException;
        }

        private JsonDictionary ParseObject(JsonReader reader)
        {
            JsonDictionary parsedDictionary = new JsonDictionary();
            while (reader.Read())
            {
                var tokenType = reader.TokenType;
                // Object ended, return closed object
                if (tokenType == JsonToken.EndObject)
                    return parsedDictionary;
                if (tokenType != JsonToken.PropertyName)
                    throw new TypeLoadException("Should be a property!");
                var key = reader.Value as string;
                // We have the key, move to the value
                if (!reader.Read())
                    throw IncompleteJsonException;
                parsedDictionary.Add(key, ParseValue(reader));
            }

            // Shouldn't get here
            throw IncompleteJsonException;
        }

        private object ParseValue(JsonReader reader)
        {
            var tokenType = reader.TokenType;
            switch (tokenType)
            {
                case JsonToken.StartObject:
                    return ParseObject(reader);
                case JsonToken.StartArray:
                    return ParseArray(reader);
                case JsonToken.String:
                case JsonToken.Float:
                case JsonToken.Integer:
                case JsonToken.Boolean:
                case JsonToken.Null:
                    return reader.Value;
                case JsonToken.Comment:
                    throw new FormatException("Comments are not supported in this parser!");
                default:
                    throw new FormatException(string.Format("Token type {0} is not supported", tokenType));
            }
        }

        private JsonList ParseArray(JsonReader reader)
        {
            var parsedArray = new JsonList();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    return parsedArray;
                parsedArray.Add(ParseValue(reader));
            }

            throw IncompleteJsonException;
        }

        #endregion
    }
}