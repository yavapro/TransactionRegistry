using System.Collections.Generic;

namespace TransactionRegistry.Core
{
    using System.Collections.Concurrent;
    using TransactionRegistry.Core.Models;
    
    public class Data
    {
        public ConcurrentDictionary<string, ServiceState> Key;

        public SortedSet<ServiceState> List;
    }
}