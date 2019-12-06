# Introduction 
This is the repository that contains AzureStorageBackup functionality.

# Getting Started
1. Pull code
2. Restore packages
3. Build solution

# Build and Test
Run dotnet pack to generate a nuget package

# How to restore table storage
This cannot be done using the package. 
    AzCopy /Source:https://myaccount.blob.core.windows.net/mycontainer/
            /SourceKey:key1
            /Dest:https://myaccount.table.core.windows.net/mytable/ 
            /DestKey:key2 
            /Manifest:"myaccount_mytable_20140103T112020.manifest"
            /EntityOperation:InsertOrMerge

# How to create a backup

## Initialize the backup storage client
* log should either be null or an implementation of ILogger
* sourceAccountName: the name of the storage account, no connectionstring or url
* sourceAccountKey: the SAS key as can be retrieved for the Azure Portal

var backupAzureStorage = new Luminis.AzureStorageBackup.BackupAzureStorage(log, sourceAccountName, sourceAccountKey);

## Backup table storage
* sourceTables: a list of string (ie: new [] { "table1", "table2" })
* destinationAccountName: the name of the storage account, no connectionstring or url
* destinationAccountKey: the SAS key as can be retrieved for the Azure Portal
* destinationContainerName: the name of the container where the backup should go to. This container is created if it does not exist yet.

await backupAzureStorage.BackupAzureTablesToBlobStorage(sourceTables, destinationAccountName, destinationKey, destinationContainerName);

## Backup blob storage
* sourceContainers: a list of string (ie: new [] { "container1", "container2" })
* destinationAccountName: the name of the storage account, no connectionstring or url
* destinationAccountKey: the SAS key as can be retrieved for the Azure Portal
* destinationContainerName: the name of the container where the backup should go to. This container is created if it does not exist yet.

await backupAzureStorage.BackupBlobStorage(containers, destinationAccountName, destinationKey, destinationContainerName);