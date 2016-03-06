using System;
using log4net.Util;
using Newtonsoft.Json;

namespace log4net.ElasticSearch.JsonConverters
{
    class LogicalThreadContextStackConverter : JsonConverter, ICustomJsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var stack = value as LogicalThreadContextStack;
            writer.WriteStartArray();
            this.Process(stack, writer, serializer);
            writer.WriteEndArray();
        }

        private void Process(LogicalThreadContextStack stack, JsonWriter writer, JsonSerializer serializer)
        {
            int count = stack.Count;
            for (int i = 0; i < count; i++)
            {
                string item = stack.Pop();
                writer.WriteValue(item);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof (LogicalThreadContextStack).IsAssignableFrom(objectType);
        }
    }
}
