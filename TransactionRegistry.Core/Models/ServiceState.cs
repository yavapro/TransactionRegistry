namespace TransactionRegistry.Core.Models
{
    public struct ServiceState
    {
        public string Id;

        public ulong ProcessedTransactionNumber;

        public ServiceState(string id, ulong processedTransactionNumber)
        {
            this.Id = id;
            this.ProcessedTransactionNumber = processedTransactionNumber;
        }
    }
}