using System;
using System.Collections.Generic;

namespace ABCRetail.StorageDemo.Models
{
    // This is the main container for all our data
    public class DataViewModel
    {
        public List<CustomerEntity> Customers { get; set; } = new List<CustomerEntity>();
        public List<ProductEntity> Products { get; set; } = new List<ProductEntity>();
        public List<BlobInfo> Blobs { get; set; } = new List<BlobInfo>();
        public List<FileInfo> Files { get; set; } = new List<FileInfo>();
        public List<string> QueueMessages { get; set; } = new List<string>();
    }

    // Class to hold blob information
    public class BlobInfo
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public long Size { get; set; }
        public DateTimeOffset LastModified { get; set; }

        // Helper property to format size for display
        public string FormattedSize => FormatBytes(Size);

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n2} {suffixes[counter]}";
        }
    }

    // Class to hold file information (for Azure Files)
    public class FileInfo
    {
        public string Name { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }
}