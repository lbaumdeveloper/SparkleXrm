using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SparkleXrm.Tasks.Config
{
    /// <summary>
    /// Reads a JSON array of strings. A comma-separated string from older spkl.json files is also accepted.
    /// </summary>
    public class StringListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<string>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var path = reader.Path;
            var token = JToken.Load(reader);

            switch (token.Type)
            {
                case JTokenType.Null:
                    return null;
                case JTokenType.String:
                    return Split(token.Value<string>());
                case JTokenType.Array:
                    return token.Values<string>().Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
                default:
                    throw new JsonSerializationException($"Expected an array of strings at '{path}'.");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        private static List<string> Split(string value)
        {
            return value.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
        }
    }
}
