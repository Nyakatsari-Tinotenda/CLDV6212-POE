using ABCRetail.StorageDemo.Services; // make sure this matches where you place StorageContext

namespace ABCRetail.StorageDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ----- Azure Storage connection -----
            string storageConn =
                Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") // for Azure App Service
                ?? builder.Configuration.GetConnectionString("Storage");             // for local dev

            // Register our custom storage context as a singleton
            builder.Services.AddSingleton(new StorageContext(storageConn));

            // Add MVC support
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();

            // Default route → StorageController/Index
            app.MapControllerRoute(
     name: "default",
     pattern: "{controller=Storage}/{action=Index}/{id?}");


            app.Run();
        }
    }
}
