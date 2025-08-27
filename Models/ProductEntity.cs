using Azure.Data.Tables;
using Azure;

namespace ABCRetail.StorageDemo.Models
{
    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public decimal Price { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
