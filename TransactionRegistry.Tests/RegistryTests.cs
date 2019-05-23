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
            Assert.Equal(1, result.Count());
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
            Assert.Equal(1, result.Count());
            Assert.Equal((ulong)maxValue, result.First().ProcessedTransactionNumber);
        }
        
        [Fact]
        public void TestSimpleManyThreadsManyDevices()
        {
            var registry = new Registry();
            var group = "Group_1";
            var maxValue = 10000;
            var devicesCount = 100;

            List<Task> tasks = new List<Task>();
            
            foreach (var n in Enumerable.Range(1, devicesCount))
            {
                var index = n;
                tasks.AddRange(Enumerable
                    .Range(1, maxValue)
                    .Select(x =>
                        Task.Factory.StartNew(() =>
                            registry.Save(group, new ServiceState("device_" + index, (ulong) x)))));
            }
            
            Task.WaitAll(tasks.ToArray());

            var result = registry.Find(group, 0L);
            
            Assert.NotNull(result);
            Assert.Equal(devicesCount, result.Count());
            Assert.Equal(devicesCount, result.Count(e => e.ProcessedTransactionNumber == (ulong)maxValue));
        }
    }
}