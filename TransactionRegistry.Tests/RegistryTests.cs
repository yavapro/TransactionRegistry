using System.Threading;

namespace TransactionRegistry.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TransactionRegistry.Core;
    using TransactionRegistry.Core.Models;
    using Xunit;
    
    public class RegistryTests
    {
        [Fact]
        public void TestEmpty()
        {
            var registry = new Registry();
            var group = "Group_1";

            var result = registry.Find(group, 0UL);
            
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public void TestSaveAndFindIt()
        {
            var registry = new Registry();
            var group = "Group_1";
            var device = "device_1";

            registry.Save(group, new ServiceState(device, (ulong) 1UL));
            
            var result = registry.Find(group, 0UL);
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1UL, result.First().ProcessedTransactionNumber);
        }
        
        [Fact]
        public void TestSaveOnlyLargerNumber()
        {
            var registry = new Registry();
            var group = "Group_1";
            var device = "device_1";

            registry.Save(group, new ServiceState(device, (ulong) 2UL));
            registry.Save(group, new ServiceState(device, (ulong) 1UL));
            

            var result = registry.Find(group, 1UL);
            
            Assert.Equal(2UL, result.First().ProcessedTransactionNumber);
        }
        
        [Fact]
        public void TestSimpleOneThreadOneDevice()
        {
            var registry = new Registry();
            var group = "Group_1";
            var device = "device_1";
            var maxValue = 100000;

            foreach (var x in Enumerable.Range(1, maxValue))
            {
                registry.Save(group, new ServiceState(device, (ulong) x));
            }

            var result = registry.Find(group, 1L);
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal((ulong)maxValue, result.First().ProcessedTransactionNumber);
        }
        
        [Fact]
        public void TestSimpleManyThreadsOneDevice()
        {
            var registry = new Registry();
            var group = "Group_1";
            var device = "device_1";
            var maxValue = 100000;
            
            var tasks = Enumerable
                .Range(1, maxValue)
                .Select(x => 
                    Task.Factory.StartNew(() =>
                        registry.Save(group, new ServiceState(device, (ulong)x))))
                .ToArray();
            
            Task.WaitAll(tasks);

            var result = registry.Find(group, 0L);
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal((ulong)maxValue, result.First().ProcessedTransactionNumber);
        }
        
        [Fact]
        public void TestSimpleManyThreadsManyDevices()
        {
            var registry = new Registry();
            var group = "Group_1";
            var maxValue = 1000;
            var devicesCount = 100;

            List<Task> tasks = new List<Task>();
            
            foreach (var device in Enumerable.Range(1, devicesCount))
            {
                var deviceIndex = device;
                tasks.AddRange(Enumerable
                    .Range(1, maxValue)
                    .Select(x =>
                        Task.Factory.StartNew(() =>
                            registry.Save(group, new ServiceState("device_" + deviceIndex, (ulong) x)))));
            }
            
            Task.WaitAll(tasks.ToArray());

            var result = registry.Find(group, 0L);
            
            Assert.NotNull(result);
            Assert.Equal(devicesCount, result.Count());
            Assert.Equal(devicesCount, result.Count(e => e.ProcessedTransactionNumber == (ulong)maxValue));
        }
        
        [Fact]
        public void TestSimpleManyThreadsManyDevicesManyGroups()
        {
            var registry = new Registry();
            var maxValue = 100;
            var devicesCount = 10;
            var groupsCount = 5;

            List<Task> tasks = new List<Task>();

            foreach (var group in Enumerable.Range(1, groupsCount))
            {
                var groupIndex = group;
                foreach (var device in Enumerable.Range(1, devicesCount))
                {
                    var deviceIndex = device;
                    tasks.AddRange(Enumerable
                        .Range(1, maxValue)
                        .Select(x =>
                            Task.Factory.StartNew(() =>
                            {
                                Thread.Sleep(200 - groupIndex * x / 10 + deviceIndex);
                                registry.Save("Group_" + groupIndex, new ServiceState("device_" + deviceIndex, (ulong) x));
                            })));
                }
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var group in Enumerable.Range(1, groupsCount))
            {
                var result = registry.Find("Group_" + group, 0L);
            
                Assert.NotNull(result);
                Assert.Equal(devicesCount, result.Count());
                Assert.Equal(devicesCount, result.Count(e => e.ProcessedTransactionNumber == (ulong)maxValue));
            }
        }
        
        [Fact]
        public void TestOneDeviceWithManySenders()
        {
            var registry = new Registry();
            var group = "Group_1";
            var device = "device_1";
            var maxValue = 10000;
            
            IEnumerable<Task> tasks = new List<Task>();
            IList<Task> tasksFactory = new List<Task>();

            tasksFactory.Add(Task.Factory.StartNew(() => tasks = CreateSenders(group, device, maxValue, registry)));
            tasksFactory.Add(Task.Factory.StartNew(() => tasks.Union(CreateSenders(group, device, maxValue, registry))));
            tasksFactory.Add(Task.Factory.StartNew(() => tasks.Union(CreateSenders(group, device, maxValue, registry))));
            
            Task.WaitAll(tasksFactory.ToArray());
            Task.WaitAll(tasks.ToArray());

            var result = registry.Find(group, 0L);
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal((ulong)maxValue, result.First().ProcessedTransactionNumber);
        }

        private IEnumerable<Task> CreateSenders(string group, string device, int maxValue, Registry registry)
        {
            return Enumerable
                .Range(1, maxValue)
                .Select(x => 
                    Task.Factory.StartNew(() =>
                        registry.Save(group, new ServiceState(device, (ulong)x))));
        }
        
        [Fact]
        public void TestFindWorkInParallelToSave()
        {
            var registry = new Registry();
            var group = "Group_1";
            var device = "device_1";
            var maxValue = 10000;

            List<Task> tasks = new List<Task>();
            
            registry.Save(group, new ServiceState(device, (ulong) 0UL));
            
            tasks.AddRange(Enumerable
                .Range(1, maxValue)
                .Select(x =>
                    Task.Factory.StartNew(() =>
                        registry.Save(group, new ServiceState(device, (ulong) x)))));

            var lastValue = 0UL;
            while (tasks.Any(t => !t.IsCompleted))
            {
                var result = registry.Find(group, 0L);
                
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.True(result.First().ProcessedTransactionNumber >= lastValue);
                
                lastValue = result.First().ProcessedTransactionNumber;
            }
            
            Task.WaitAll(tasks.ToArray());
        }
    }
}