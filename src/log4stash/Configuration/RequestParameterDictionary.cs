using System.Collections;
using System.Collections.Generic;

namespace log4stash.Configuration
{
    public class RequestParameterDictionary : IDictionary<string, string>
    {
        private readonly IDictionary<string, string> _parametersDictionary;

        public RequestParameterDictionary()
        {
            _parametersDictionary = new Dictionary<string, string>();
        }

        public void AddParameter(RequestParameter parameter)
        {
            _parametersDictionary[parameter.Key] = parameter.Value;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _parametersDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _parametersDictionary).GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _parametersDictionary.Add(item);
        }

        public void Clear()
        {
            _parametersDictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _parametersDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _parametersDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _parametersDictionary.Remove(item);
        }

        public int Count
        {
            get { return _parametersDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return _parametersDictionary.IsReadOnly; }
        }

        public bool ContainsKey(string key)
        {
            return _parametersDictionary.ContainsKey(key);
        }

        public void Add(string key, string value)
        {
            _parametersDictionary.Add(key, value);
        }

        public bool Remove(string key)
        {
            return _parametersDictionary.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _parametersDictionary.TryGetValue(key, out value);
        }

        public string this[string key]
        {
            get { return _parametersDictionary[key]; }
            set { _parametersDictionary[key] = value; }
        }

        public ICollection<string> Keys
        {
            get { return _parametersDictionary.Keys; }
        }

        public ICollection<string> Values
        {
            get { return _parametersDictionary.Values; }
        }
    }
}
