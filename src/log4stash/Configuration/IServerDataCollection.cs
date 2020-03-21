using System.Collections.Generic;

namespace log4stash.Configuration
{
    public interface IServerDataCollection : IList<IServerData>
    {
        void AddServerData(IServerData serverData);
        void AddServer(ServerData serverData);
        IServerData GetRandomServerData();
    }
}