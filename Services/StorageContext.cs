using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;

namespace ABCRetail.StorageDemo.Services // <- MAKE SURE THIS NAMESPACE IS CORRECT
{
    public class StorageContext
    {
        private readonly string _connectionString;

        // Service clients
        public TableServiceClient TableService { get; }
        public BlobServiceClient BlobService { get; }
        public QueueServiceClient QueueService { get; }
        public ShareServiceClient FileService { get; }

        // Pre-defined names for your resources
        public const string CustomersTableName = "Customers";
        public const string ProductsTableName = "Products";
        public const string MediaContainerName = "media";
        public const string OrdersQueueName = "orders";
        public const string ContractsShareName = "contracts";

        public StorageContext(string connectionString)
        {
            _connectionString = connectionString;
            // Initialize the service clients
            TableService = new TableServiceClient(connectionString);
            BlobService = new BlobServiceClient(connectionString);
            QueueService = new QueueServiceClient(connectionString);
            FileService = new ShareServiceClient(connectionString);
        }

        // ----- Helper Methods to get clients AND ensure resources exist -----

        public async Task<TableClient> GetCustomersTableAsync()
        {
            var tableClient = TableService.GetTableClient(CustomersTableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        public async Task<TableClient> GetProductsTableAsync()
        {
            var tableClient = TableService.GetTableClient(ProductsTableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        public async Task<BlobContainerClient> GetMediaContainerAsync()
        {
            var containerClient = BlobService.GetBlobContainerClient(MediaContainerName);
            await containerClient.CreateIfNotExistsAsync();
            // Set public access level so images can be displayed in the browser
            await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
            return containerClient;
        }

        public async Task<QueueClient> GetOrdersQueueAsync()
        {
            var queueClient = QueueService.GetQueueClient(OrdersQueueName);
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }

        public async Task<ShareClient> GetContractsShareAsync()
        {
            var shareClient = FileService.GetShareClient(ContractsShareName);
            await shareClient.CreateIfNotExistsAsync();
            return shareClient;
        }
    }
}