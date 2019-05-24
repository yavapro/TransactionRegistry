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
            var storyService = GetServiceStory(serviceType);
            var newTransactionNumber = UpdateDeviceTransactionNumber(storyService, serviceState);

            if (newTransactionNumber.ProcessedTransactionNumber == serviceState.ProcessedTransactionNumber)
            {
                UpdateSearchList(storyService, serviceState);
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

        private Data GetServiceStory(string serviceType)
        {
            return story.GetOrAdd(
                serviceType, 
                new Data
                {
                    Key = new ConcurrentDictionary<string, ServiceState>(), 
                    List = new SortedSet<ServiceState>()
                });
        }

        private ServiceState UpdateDeviceTransactionNumber(Data serviceStory, ServiceState serviceState)
        {
            return serviceStory.Key.AddOrUpdate(
                serviceState.Id,
                serviceState,
                (key, value) => value.ProcessedTransactionNumber > serviceState.ProcessedTransactionNumber ? value : serviceState);
        }

        private void UpdateSearchList(Data serviceStory, ServiceState serviceState)
        {
            SortedSet<ServiceState> savedList, newList;
            
            do
            {
                savedList = Interlocked.CompareExchange(ref serviceStory.List, null, null);
                newList = new SortedSet<ServiceState>(serviceStory.Key.Values, new ServiceStateComparer());

                if (IsDeviceTransactionNumberInvalidate(serviceStory, serviceState))
                {
                    break;
                }
                    
            } while (Interlocked.CompareExchange(ref serviceStory.List, newList, savedList) != savedList);
        }

        private bool IsDeviceTransactionNumberInvalidate(Data serviceStory, ServiceState serviceState)
        {
            var currentServiceState = serviceStory.Key.GetOrAdd(serviceState.Id, serviceState);
            return currentServiceState.ProcessedTransactionNumber > serviceState.ProcessedTransactionNumber;
        }
    }
}