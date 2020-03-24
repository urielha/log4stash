using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4stash.Filters;
using log4stash.InnerExceptions;

namespace log4stash
{
    public class ElasticAppenderFilters : IElasticAppenderFilter
    {
        private readonly List<IElasticAppenderFilter> _filters = new List<IElasticAppenderFilter>();

        public ElasticAppenderFilters()
        {
        }

        public ElasticAppenderFilters(List<IElasticAppenderFilter> filters)
        {
            _filters = filters;
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
            foreach (var filter in _filters)
            {
                ValidateFilterProperties(filter);
                filter.PrepareConfiguration(client);
            }
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            foreach (var filter in _filters)
            {
                filter.PrepareEvent(logEvent);
            }
        }

        public void AddFilter(IElasticAppenderFilter filter)
        {
            _filters.Add(filter);
        }
        
        public static void ValidateFilterProperties(IElasticAppenderFilter filter)
        {
            var invalidProperties =
                filter.GetType().GetProperties()
                    .Where(prop => !IsValidProperty(prop, filter))
                    .Select(p => p.Name).ToList();

            if (invalidProperties.Any())
            {
                var properties = string.Join(",", invalidProperties.ToArray());
                throw new InvalidFilterConfigurationException(
                    string.Format("The properties ({0}) of {1} are invalid.", properties, filter.GetType().Name));
            }
        }

        private static bool IsValidProperty(PropertyInfo prop, IElasticAppenderFilter filter)
        {
            var validation = prop.GetCustomAttributes(typeof (IPropertyValidationAttribute), true).FirstOrDefault() as IPropertyValidationAttribute;
            if (validation == null)
            {
                return true;
            }

            return validation.IsValid(prop.GetValue(filter, null));
        }

        #region Helpers for common filters

        public void AddAdd(AddValueFilter filter)
        {
            AddFilter(filter);
        }

        public void AddRemove(RemoveKeyFilter filter)
        {
            AddFilter(filter);
        }

        public void AddRename(RenameKeyFilter filter)
        {
            AddFilter(filter);
        }

        public void AddKv(KvFilter filter)
        {
            AddFilter(filter);
        }

        public void AddGrok(GrokFilter filter)
        {
            AddFilter(filter);
        }

        public void AddConvertToArray(ConvertToArrayFilter filter)
        {
            AddFilter(filter);
        }

        public void AddJson(JsonFilter filter)
        {
            AddFilter(filter);
        }

        public void AddXml(XmlFilter filter)
        {
            AddFilter(filter);
        }

        public void AddConvert(ConvertFilter filter)
        {
            AddFilter(filter);
        }

        #endregion
    }

    public interface IPropertyValidationAttribute
    {
        bool IsValid<T>(T value);
    }

    public class PropertyNotEmptyAttribute : Attribute, IPropertyValidationAttribute
    {
        public bool IsValid<T>(T value)
        {
            return InnerIsValid(value as string);
        }

        private bool InnerIsValid(string value)
        {
            return !string.IsNullOrEmpty(value);
        }
    }
}