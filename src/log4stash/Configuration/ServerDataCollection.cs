using System;
using System.Collections;
using System.Collections.Generic;

namespace log4stash.Configuration
{
    public class ServerDataCollection : IList<IServerData>
    {
        private readonly IList<IServerData> _serverDatas;

        public ServerDataCollection()
        {
            _serverDatas = new List<IServerData>();
        }

        public void AddServerData(IServerData serverData)
        {
            _serverDatas.Add(serverData);
        }

        public void AddServer(ServerData serverData)
        {
            AddServerData(serverData);
        }

        public IServerData GetRandomServerData()
        {
            var now = DateTime.Now.Ticks;
            var index = (int) (now %_serverDatas.Count);
            return _serverDatas[index];
        }

        public IEnumerator<IServerData> GetEnumerator()
        {
            return _serverDatas.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _serverDatas).GetEnumerator();
        }

        public void Add(IServerData item)
        {
            _serverDatas.Add(item);
        }

        public void Clear()
        {
            _serverDatas.Clear();
        }

        public bool Contains(IServerData item)
        {
            return _serverDatas.Contains(item);
        }

        public void CopyTo(IServerData[] array, int arrayIndex)
        {
            _serverDatas.CopyTo(array, arrayIndex);
        }

        public bool Remove(IServerData item)
        {
            return _serverDatas.Remove(item);
        }

        public int Count
        {
            get { return _serverDatas.Count; }
        }

        public bool IsReadOnly
        {
            get { return _serverDatas.IsReadOnly; }
        }

        public int IndexOf(IServerData item)
        {
            return _serverDatas.IndexOf(item);
        }

        public void Insert(int index, IServerData item)
        {
            _serverDatas.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _serverDatas.RemoveAt(index);
        }

        public IServerData this[int index]
        {
            get { return _serverDatas[index]; }
            set { _serverDatas[index] = value; }
        }
    }
}