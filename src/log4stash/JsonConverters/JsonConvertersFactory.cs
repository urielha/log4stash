using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace log4stash.JsonConverters
{
    /// <summary>
    /// This interface is just a place holder.
    /// All log4stash' custom json converters should implement this interface
    /// in order to be find throught the <see cref="JsonConvertersFactory"/>.
    /// </summary>
    interface ICustomJsonConverter
    {
     
    }

    class JsonConvertersFactory
    {
        public static JsonConverter[] GetCustomConverters()
        {
            var converters = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.Namespace != null && t.IsClass
                            && typeof(ICustomJsonConverter).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(Activator.CreateInstance).OfType<JsonConverter>();
            return converters.ToArray();
        }
    }
}
