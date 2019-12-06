namespace Luminis.AzureStorageBackup
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class BackupAzureStorage
    {
        private const string AzCopy = "azcopy.exe";
        private const int AzCopyDelay = 1000;

        private readonly ILogger logger;
        private readonly string sourceAccountName;
        private readonly string sourceAccountKey;
        private static CloudBlobContainer blobContainer;
        private readonly string locationOfAzCopy = Path.Combine(Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName, "azcopy", AzCopy);

        /// <summary>
        /// Constructor for BackupAzureStorage.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="sourceAccountName">The name of the storage account that holds the tables and blobs that should be backuped.</param>
        /// <param name="sourceKey">The key of the storage account.</param>
        public BackupAzureStorage(ILogger logger, string sourceAccountName, string sourceAccountKey)
        {
            if (string.IsNullOrWhiteSpace(sourceAccountName)) throw new ArgumentNullException(nameof(sourceAccountName));
            if (string.IsNullOrWhiteSpace(sourceAccountKey)) throw new ArgumentNullException(nameof(sourceAccountKey));
            this.logger = logger;
            this.sourceAccountName = sourceAccountName;
            this.sourceAccountKey = sourceAccountKey;
        }

        /// <summary>
        /// Backup Azure Tables to BlobStorage.
        /// </summary>
        /// <param name="sourceTables">The tables to backup.</param>
        /// <param name="targetAccountName">The name of the storage account to backup to.</param>
        /// <param name="targetAccountKey">The (SAS) key of the storage account to backup to.</param>
        /// <param name="targetContainerName">The name of the container to backup to.</param>
        /// <returns></returns>
        public async Task BackupAzureTablesToBlobStorage(IEnumerable<string> sourceTables, string targetAccountName, string targetAccountKey, string targetContainerName)
        {
            if (sourceTables == null || !sourceTables.Any()) throw new ArgumentNullException(nameof(sourceTables));
            if (string.IsNullOrWhiteSpace(targetAccountName)) throw new ArgumentNullException(nameof(targetAccountName));
            if (string.IsNullOrWhiteSpace(targetAccountKey)) throw new ArgumentNullException(nameof(targetAccountKey));
            if (string.IsNullOrWhiteSpace(targetContainerName)) throw new ArgumentNullException(nameof(targetContainerName));

            await CreateBlobContainerIfItDoesntExist(targetAccountName, targetAccountKey, targetContainerName);

            logger?.LogInformation($"BackupAzureTablesToBlobStorage - From: {sourceAccountName}, Tables: {string.Join(", ", sourceTables)} to {targetAccountName}/{targetContainerName}");

            foreach (var sourceTable in sourceTables)
            {
                var arguments = $@"/source:https://{sourceAccountName}.table.core.windows.net/{sourceTable} /sourceKey:{sourceAccountKey} /dest:https://{targetAccountName}.blob.core.windows.net/{targetContainerName}/ /Destkey:{targetAccountKey} /Y";

                await RunAzCopyAsync(locationOfAzCopy, arguments);
            }
            logger?.LogInformation("BackupAzureTablesToBlobStorage - Done");
        }

        /// <summary>
        /// Backup BlobStorage Containers to BlobStorage.
        /// </summary>
        /// <param name="sourceContainers">The containers to backup.</param>
        /// <param name="targetAccountName">The name of the storage account to backup to.</param>
        /// <param name="targetAccountKey">The (SAS) key of the storage account to backup to.</param>
        /// <param name="targetContainerName">The name of the container to backup to.</param>
        /// <returns></returns>
        public async Task BackupBlobStorage(IEnumerable<string> sourceContainers, string targetAccountName, string targetAccountKey, string targetContainerName)
        {
            if (sourceContainers == null || !sourceContainers.Any()) throw new ArgumentNullException(nameof(sourceContainers));
            if (string.IsNullOrWhiteSpace(targetAccountName)) throw new ArgumentNullException(nameof(targetAccountName));
            if (string.IsNullOrWhiteSpace(targetAccountKey)) throw new ArgumentNullException(nameof(targetAccountKey));
            if (string.IsNullOrWhiteSpace(targetContainerName)) throw new ArgumentNullException(nameof(targetContainerName));

            await CreateBlobContainerIfItDoesntExist(targetAccountName, targetAccountKey, targetContainerName);

            logger?.LogInformation($"BackupBlobStorage - From: {sourceAccountName}, Containers: {string.Join(", ", sourceContainers)} to {targetAccountName}/{targetContainerName}");

            foreach (var sourcContainer in sourceContainers)
            {
                var arguments = $@"/source:https://{sourceAccountName}.blob.core.windows.net/{sourcContainer} /sourceKey:{sourceAccountKey} /dest:https://{targetAccountName}.blob.core.windows.net/{targetContainerName}/ /Destkey:{targetAccountKey} /S /Y";
                await RunAzCopyAsync(locationOfAzCopy, arguments);
            }
            logger?.LogInformation("Done");
        }

        private async Task RunAzCopyAsync(string locationOfAzCopy, string arguments)
        {
            using (var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = locationOfAzCopy,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            })
            {
                proc.Start();
                proc.WaitForExit();

                var infoMessage = GetMessageFromOutput(proc.StandardOutput);
                var errorMessage = GetMessageFromOutput(proc.StandardError);

                if (!string.IsNullOrEmpty(infoMessage))
                {
                    logger?.LogInformation(infoMessage);
                }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    logger?.LogError(errorMessage);
                    // An error was logged by the external program. Throw this as exception for this task.
                    throw new OperationCanceledException(errorMessage);
                }
            }
            await Task.Delay(AzCopyDelay);
        }

        private static string GetMessageFromOutput(StreamReader outputStream)
        {
            var parts = new List<string>();
            while (!outputStream.EndOfStream)
            {
                parts.Add(outputStream.ReadToEnd());
            }
            return string.Join(Environment.NewLine, parts);
        }

        private static async Task CreateBlobContainerIfItDoesntExist(string targetAccountName, string targetAccountKey, string containerName)
        {
            if (blobContainer == null)
            {
                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={targetAccountName};AccountKey={targetAccountKey};EndpointSuffix=core.windows.net";
                var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);

                var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                blobContainer = cloudBlobClient.GetContainerReference(containerName);
                await blobContainer.CreateIfNotExistsAsync();
            }
        }
    }
}
