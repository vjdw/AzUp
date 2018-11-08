using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzUp
{
    class Program
    {
        private static CloudBlobClient m_blobClient;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.local.json", optional : true);
                var config = builder.Build();

                var connectionString = config["StorageConnectionString"];
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                m_blobClient = storageAccount.CreateCloudBlobClient();
                var targetBlobContainer = m_blobClient.GetContainerReference("archive");

                var fileUploader = new FileUploader(config["SourceDirectory"], targetBlobContainer);
                await fileUploader.Run();

                Console.WriteLine("Finished");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}