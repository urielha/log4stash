using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace log4net.ElasticSearch.JsonConverters
{
    public class StringJsonConverter : JsonConverter, ICustomJsonConverter
    {
        public class JsonString
        {
            public string Value { get; private set; }

            public JsonString(string value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // need to escape "\n" in order to not ruine the Elastic protocol
            writer.WriteRawValue(value.ToString().Replace("\r\n", "").Replace("\n",""));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonString).IsAssignableFrom(objectType);
        }

        public override bool CanRead
        {
            get { return false; }
        }
    }
}
