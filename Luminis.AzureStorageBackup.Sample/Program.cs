using Luminis.AzureStorageBackup;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace SampleApp
{
    class Program
    {
        public static string StorageAccountName { get; private set; } = "--AccountName--";
        public static string StorageAccountKey { get; private set; } = "--StorageAccountKey--";
        public static string[] SourceTables { get; private set; } = new string[] { "sourceTable" };
        public static string[] SourceContainers { get; private set; } = new string[] { "sourcecontainer" };

        public static string SourceConnectionString = $"DefaultEndpointsProtocol=http;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
        

        static void Main()
        {
            try
            {
                Seed().Wait();
                BackupTableAndBlob().Wait();
            }
            catch (Exception e) 
            {
                Console.WriteLine(e);
            }
        }

        private static async Task Seed()
        {
            await SeedTableEntity();
            await SeedBlob();

        }
        private static async Task SeedBlob()
        {
            var storageAccount = Microsoft.Azure.Storage.CloudStorageAccount.Parse(SourceConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            foreach (var containerName in SourceContainers)
            {
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                await container.CreateIfNotExistsAsync();
                CloudBlockBlob blob = container.GetBlockBlobReference("sampleblob.txt");
                await blob.UploadTextAsync("This is a blob.");
            }
        }

        private static async Task SeedTableEntity()
        {
            var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(SourceConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            foreach (var table in SourceTables)
            {
                CloudTable cloudTable = tableClient.GetTableReference(table);
                await cloudTable.CreateIfNotExistsAsync();
                var customer = new CustomerEntity("Frank", "Folsche") { };
                var insertOrMergeOperation = TableOperation.InsertOrMerge(customer);
                await cloudTable.ExecuteAsync(insertOrMergeOperation);
            }
        }

        private static async Task BackupTableAndBlob()
        {
            // When using NuGet azcopy will be added to the project. now we are referencing the project directly so we can find the azcopy here:
            var azCopyPath = Assembly.GetExecutingAssembly().Location + @"..\..\..\..\..\..\Luminis.AzureStorageBackup\lib\AzCopy\";
            var backupAzureStorage = new BackupAzureStorage(StorageAccountName, StorageAccountKey, null, azCopyPath);
            await backupAzureStorage.BackupAzureTablesToBlobStorage(SourceTables, StorageAccountName, StorageAccountKey, "destinationcontainer", "tables");
            await backupAzureStorage.BackupBlobStorage(SourceContainers, StorageAccountName, StorageAccountKey, "destinationcontainer", "blobs");
        }
    }
    public class CustomerEntity : TableEntity
    {
        public CustomerEntity(string lastName, string firstName)
        {
            PartitionKey = lastName;
            RowKey = firstName;
        }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
