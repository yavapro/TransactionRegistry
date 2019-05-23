namespace TransactionRegistry.Core
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using TransactionRegistry.Core.Models;
    using System.Threading;
    
    public class Registry
    {
        private readonly ConcurrentDictionary<string, Data> story = new ConcurrentDictionary<string, Data>();
    
        public void Save(string serviceType, ServiceState serviceState)
        {
            var storyService = story.GetOrAdd(
                serviceType, 
                new Data
                {
                    Key = new ConcurrentDictionary<string, ServiceState>(), 
                    List = new SortedSet<ServiceState>()
                });
            
            var newTransactionNumber = storyService.Key.AddOrUpdate(
                serviceState.Id,
                serviceState,
                (key, value) => value.ProcessedTransactionNumber > serviceState.ProcessedTransactionNumber ? value : serviceState);

            if (newTransactionNumber.ProcessedTransactionNumber == serviceState.ProcessedTransactionNumber)
            {
                SortedSet<ServiceState> savedList, newList;
                
                do
                {
                    savedList = Interlocked.CompareExchange(ref storyService.List, null, null);
                    newList = new SortedSet<ServiceState>(storyService.Key.Values, new ServiceStateComparer());
                } while (Interlocked.CompareExchange(ref storyService.List, newList, savedList) != savedList);
            }
        }

        public IEnumerable<ServiceState> Find(string serviceType, ulong transactionNumberFrom)
        {
            if (!story.ContainsKey(serviceType))
            {
                return new ServiceState[0];
            }
            
            var savedList = Interlocked.CompareExchange(ref story[serviceType].List, null, null);
            
            var list = savedList.GetViewBetween(
                new ServiceState(null, transactionNumberFrom),
                new ServiceState(null, ulong.MaxValue));

            return list;
        }
        
        
    }
}