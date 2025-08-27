using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares.Models;
using System.Text;
using ABCRetail.StorageDemo.Models;
using ABCRetail.StorageDemo.Services;
using System.IO; // This causes conflict with FileInfo - we need to use fully qualified names

namespace ABCRetail.StorageDemo.Controllers
{
    public class StorageController : Controller
    {
        private readonly StorageContext _ctx;
        private readonly ILogger<StorageController> _logger;

        // Constructor with dependency injection
        public StorageController(StorageContext ctx, ILogger<StorageController> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        // ---------- TABLES ----------
        [HttpPost]
        public async Task<IActionResult> AddCustomer(string first, string last, string email)
        {
            try
            {
                var customersTable = await _ctx.GetCustomersTableAsync();
                var c = new CustomerEntity
                {
                    PartitionKey = "Customer",
                    RowKey = Guid.NewGuid().ToString(),
                    FirstName = first,
                    LastName = last,
                    Email = email
                };
                await customersTable.AddEntityAsync(c);
                TempData["Message"] = $"Customer '{first} {last}' added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer");
                TempData["Error"] = $"Failed to add customer: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(string name, string sku, decimal price)
        {
            try
            {
                var productsTable = await _ctx.GetProductsTableAsync();
                var p = new ProductEntity
                {
                    PartitionKey = "Product",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = name,
                    Sku = sku,
                    Price = price
                };
                await productsTable.AddEntityAsync(p);
                TempData["Message"] = $"Product '{name}' added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                TempData["Error"] = $"Failed to add product: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // ---------- BLOBS ----------
        [HttpPost]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("Index");
            }

            try
            {
                var mediaContainer = await _ctx.GetMediaContainerAsync();
                var blob = mediaContainer.GetBlobClient(file.FileName);
                await using var s = file.OpenReadStream();
                await blob.UploadAsync(s, overwrite: true);
                TempData["Message"] = $"File '{file.FileName}' uploaded successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading media");
                TempData["Error"] = $"Upload failed: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // ---------- QUEUES ----------
        [HttpPost]
        public async Task<IActionResult> EnqueueOrder(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                TempData["Error"] = "Order ID is required.";
                return RedirectToAction("Index");
            }

            try
            {
                var ordersQueue = await _ctx.GetOrdersQueueAsync();
                var msg = $"Processing order {orderId} at {DateTime.UtcNow:O}";
                await ordersQueue.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(msg)));
                TempData["Message"] = $"Order '{orderId}' queued for processing!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing order");
                TempData["Error"] = $"Failed to queue order: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // ---------- FILES (Azure Files) ----------
        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a contract file to upload.";
                return RedirectToAction("Index");
            }

            try
            {
                var contractsShare = await _ctx.GetContractsShareAsync();
                var dir = contractsShare.GetRootDirectoryClient();

                var shareFile = dir.GetFileClient(file.FileName);
                await shareFile.CreateAsync(file.Length);
                await using var s = file.OpenReadStream();
                await shareFile.UploadRangeAsync(new HttpRange(0, file.Length), s);
                TempData["Message"] = $"Contract '{file.FileName}' uploaded successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading contract");
                TempData["Error"] = $"Contract upload failed: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // Index: display lists for items
        public async Task<IActionResult> Index()
        {
            try
            {
                var customersTable = await _ctx.GetCustomersTableAsync();
                var productsTable = await _ctx.GetProductsTableAsync();
                var mediaContainer = await _ctx.GetMediaContainerAsync();
                var contractsShare = await _ctx.GetContractsShareAsync();

                var customers = customersTable.Query<CustomerEntity>(c => c.PartitionKey == "Customer").Take(50).ToList();
                var products = productsTable.Query<ProductEntity>(p => p.PartitionKey == "Product").Take(50).ToList();

                var blobs = new List<string>();
                await foreach (var item in mediaContainer.GetBlobsAsync())
                    blobs.Add(item.Name);

                var files = new List<string>();
                var dir = contractsShare.GetRootDirectoryClient();
                await foreach (ShareFileItem item in dir.GetFilesAndDirectoriesAsync())
                    if (!item.IsDirectory) files.Add(item.Name);

                ViewBag.Customers = customers;
                ViewBag.Products = products;
                ViewBag.Blobs = blobs;
                ViewBag.Files = files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for Index page");
                TempData["Error"] = $"Could not load data: {ex.Message}";
                ViewBag.Customers = new List<CustomerEntity>();
                ViewBag.Products = new List<ProductEntity>();
                ViewBag.Blobs = new List<string>();
                ViewBag.Files = new List<string>();
            }
            return View();
        }

        // NEW ACTION: View all data in tabs
        public async Task<IActionResult> ViewData()
        {
            var dataViewModel = new DataViewModel();

            try
            {
                var customersTable = await _ctx.GetCustomersTableAsync();
                var productsTable = await _ctx.GetProductsTableAsync();
                var mediaContainer = await _ctx.GetMediaContainerAsync();
                var ordersQueue = await _ctx.GetOrdersQueueAsync();
                var contractsShare = await _ctx.GetContractsShareAsync();

                // 1. Load Customers
                dataViewModel.Customers = customersTable.Query<CustomerEntity>(c => c.PartitionKey == "Customer")
                    .OrderBy(c => c.LastName)
                    .Take(100)
                    .ToList();

                // 2. Load Products
                dataViewModel.Products = productsTable.Query<ProductEntity>(p => p.PartitionKey == "Product")
                    .OrderBy(p => p.Name)
                    .Take(100)
                    .ToList();

                // 3. Load Blobs - Use fully qualified name to avoid ambiguity
                dataViewModel.Blobs = new List<ABCRetail.StorageDemo.Models.BlobInfo>();
                await foreach (var blobItem in mediaContainer.GetBlobsAsync())
                {
                    var blobClient = mediaContainer.GetBlobClient(blobItem.Name);
                    dataViewModel.Blobs.Add(new ABCRetail.StorageDemo.Models.BlobInfo
                    {
                        Name = blobItem.Name,
                        Url = blobClient.Uri.ToString(),
                        Size = blobItem.Properties.ContentLength ?? 0,
                        LastModified = blobItem.Properties.LastModified ?? DateTimeOffset.Now
                    });
                }
                dataViewModel.Blobs = dataViewModel.Blobs.OrderBy(b => b.Name).ToList();

                // 4. Load Files - Use fully qualified name to avoid ambiguity
                dataViewModel.Files = new List<ABCRetail.StorageDemo.Models.FileInfo>();
                var dir = contractsShare.GetRootDirectoryClient();
                await foreach (ShareFileItem item in dir.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        dataViewModel.Files.Add(new ABCRetail.StorageDemo.Models.FileInfo
                        {
                            Name = item.Name,
                            LastModified = item.Properties.LastModified ?? DateTimeOffset.Now
                        });
                    }
                }
                dataViewModel.Files = dataViewModel.Files.OrderBy(f => f.Name).ToList();

                // 5. Peek at Queue Messages
                dataViewModel.QueueMessages = new List<string>();
                var messages = await ordersQueue.PeekMessagesAsync(maxMessages: 10);
                foreach (var message in messages.Value)
                {
                    try
                    {
                        var messageBytes = Convert.FromBase64String(message.Body.ToString());
                        var decodedMessage = Encoding.UTF8.GetString(messageBytes);
                        dataViewModel.QueueMessages.Add(decodedMessage);
                    }
                    catch
                    {
                        dataViewModel.QueueMessages.Add($"Raw message: {message.Body}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for ViewData page");
                TempData["Error"] = $"Could not load all data: {ex.Message}";
            }

            return View(dataViewModel);
        }
    }
}