namespace Luminis.AzureStorageBackup
{
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class BackupAzureStorage
    {
        private const int AzCopyDelay = 1000;

        private readonly ILogger logger;
        private readonly string sourceAccountName;
        private readonly string sourceAccountKey;
        private static CloudBlobContainer blobContainer;
        private readonly string locationOfAzCopy;
        private static readonly object azcopyRunning = new object();

        /// <summary>
        /// Constructor for BackupAzureStorage.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="sourceAccountName">The name of the storage account that holds the tables and blobs that should be backuped.</param>
        /// <param name="sourceAccountKey">The key of the storage account.</param>
        /// <param name="rootDir">Optional rootdir where AzCopy can be found.</param>
        public BackupAzureStorage(ILogger logger, string sourceAccountName, string sourceAccountKey, string rootDir = null)
        {
            if (string.IsNullOrWhiteSpace(sourceAccountName))
            {
                throw new ArgumentNullException(nameof(sourceAccountName));
            }

            if (string.IsNullOrWhiteSpace(sourceAccountKey))
            {
                throw new ArgumentNullException(nameof(sourceAccountKey));
            }

            this.logger = logger;

            if (string.IsNullOrWhiteSpace(rootDir))
            {
                rootDir = Directory.GetCurrentDirectory();
            }

            this.locationOfAzCopy = Directory.GetFiles(rootDir, "azcopy.exe", SearchOption.AllDirectories).First();
            logger?.LogInformation($"Using azcopy from {this.locationOfAzCopy}.");

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
        /// <param name="subFolder">Optional subfolder where files are placed</param>
        /// <returns></returns>
        public async Task BackupAzureTablesToBlobStorage(IEnumerable<string> sourceTables, string targetAccountName, string targetAccountKey, string targetContainerName, string subFolder = null)
        {
            if (sourceTables == null || !sourceTables.Any())
            {
                throw new ArgumentNullException(nameof(sourceTables));
            }

            if (string.IsNullOrWhiteSpace(targetAccountName))
            {
                throw new ArgumentNullException(nameof(targetAccountName));
            }

            if (string.IsNullOrWhiteSpace(targetAccountKey))
            {
                throw new ArgumentNullException(nameof(targetAccountKey));
            }

            if (string.IsNullOrWhiteSpace(targetContainerName))
            {
                throw new ArgumentNullException(nameof(targetContainerName));
            }

            await CreateBlobContainerIfItDoesntExist(targetAccountName, targetAccountKey, targetContainerName);

            this.logger?.LogInformation($"BackupAzureTablesToBlobStorage - From: {this.sourceAccountName}, Tables: {string.Join(", ", sourceTables)} to {targetAccountName}/{targetContainerName}");

            foreach (var sourceTable in sourceTables)
            {
                var arguments = $@"/source:https://{this.sourceAccountName}.table.core.windows.net/{sourceTable} /sourceKey:{this.sourceAccountKey} /dest:https://{targetAccountName}.blob.core.windows.net/{targetContainerName}/{subFolder} /Destkey:{targetAccountKey} /Y";
                RunAzCopy(this.locationOfAzCopy, arguments);
            }
            this.logger?.LogInformation("BackupAzureTablesToBlobStorage - Done");
        }

        /// <summary>
        /// Backup BlobStorage Containers to BlobStorage.
        /// </summary>
        /// <param name="sourceContainers">The containers to backup.</param>
        /// <param name="targetAccountName">The name of the storage account to backup to.</param>
        /// <param name="targetAccountKey">The (SAS) key of the storage account to backup to.</param>
        /// <param name="targetContainerName">The name of the container to backup to.</param>
        /// <param name="subFolder">Optional subfolder where files are placed</param>
        /// <returns></returns>
        public async Task BackupBlobStorage(IEnumerable<string> sourceContainers, string targetAccountName, string targetAccountKey, string targetContainerName, string subFolder = null)
        {
            if (sourceContainers == null || !sourceContainers.Any())
            {
                throw new ArgumentNullException(nameof(sourceContainers));
            }

            if (string.IsNullOrWhiteSpace(targetAccountName))
            {
                throw new ArgumentNullException(nameof(targetAccountName));
            }

            if (string.IsNullOrWhiteSpace(targetAccountKey))
            {
                throw new ArgumentNullException(nameof(targetAccountKey));
            }

            if (string.IsNullOrWhiteSpace(targetContainerName))
            {
                throw new ArgumentNullException(nameof(targetContainerName));
            }

            await CreateBlobContainerIfItDoesntExist(targetAccountName, targetAccountKey, targetContainerName);

            this.logger?.LogInformation($"BackupBlobStorage - From: {this.sourceAccountName}, Containers: {string.Join(", ", sourceContainers)} to {targetAccountName}/{targetContainerName}");

            string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

            foreach (var sourceContainer in sourceContainers)
            {
                var arguments = $@"/source:https://{this.sourceAccountName}.blob.core.windows.net/{sourceContainer} /sourceKey:{this.sourceAccountKey} /dest:https://{targetAccountName}.blob.core.windows.net/{targetContainerName}/{subFolder}/{sourceContainer}/{timestamp} /Destkey:{targetAccountKey} /S /Y";
                RunAzCopy(this.locationOfAzCopy, arguments);
            }
            this.logger?.LogInformation("Done");
        }

        private void RunAzCopy(string locationOfAzCopy, string arguments)
        {
            lock (azcopyRunning)
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
                        this.logger?.LogInformation(infoMessage);
                    }

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        this.logger?.LogError(errorMessage);
                        // An error was logged by the external program. Throw this as exception for this task.
                        throw new OperationCanceledException(errorMessage);
                    }
                }
                // Delay to allow the release of the AZCopy temporary files
                Thread.Sleep(AzCopyDelay);
            }
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

                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                blobContainer = cloudBlobClient.GetContainerReference(containerName);
                await blobContainer.CreateIfNotExistsAsync();
            }
        }
    }
}
