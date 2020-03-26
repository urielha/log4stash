using System;
using System.Collections;
using System.Collections.Generic;

namespace log4stash.Extensions
{
    public static class LoggingEventExtensions
    {
        public static readonly string TagsKeyName = "@Tags";

        public static void AddOrSet(this Dictionary<string, object> loggingEvent, string key, object value)
        {
            object token;
            if (loggingEvent.TryGetValue(key, out token))
            {
                var array = token as IList;
                if (array == null)
                {
                    array = new List<object>(new[] {token});
                    loggingEvent[key] = array;
                }
                array.Add(value);
            }
            else
            {
                loggingEvent[key] = value;
            }
        }

        public static void ApplyFilter(this Dictionary<string, object> loggingEvent, IElasticAppenderFilter filter)
        {
            filter.PrepareEvent(loggingEvent);
        }

        public static void AddTag(this Dictionary<string, object> loggingEvent, string tag)
        {
            loggingEvent.AddOrSet(TagsKeyName, tag);
        }

        public static bool TryGetStringValue(this Dictionary<string, object> loggingEvent, string key, out string value)
        {
            value = String.Empty;
            object token;
            if(loggingEvent.TryGetValue(key, out token))
            {
                value = token as string;
                if (value != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}