using System;
using System.Collections.Generic;
using TransactionRegistry.Core.Models;

namespace TransactionRegistry.Core
{
    public class ServiceStateComparer : IComparer<ServiceState>
    {
        public int Compare(ServiceState x, ServiceState y)
        {
            {
                var value = x.ProcessedTransactionNumber.CompareTo(y.ProcessedTransactionNumber);

                // Так как у нас SortedSet, то чтобы он вставлял ключи с одним и тем же transactionNumber для разных service.Id
                if (value == 0)
                {
                    return String.Compare(x.Id, y.Id, StringComparison.Ordinal);
                }

                return value;
            }
        }
    }
}