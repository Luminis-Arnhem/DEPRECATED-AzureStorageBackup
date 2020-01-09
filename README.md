# .NET backup package for Azure Storage accounts
This package can be used to backup tables and blobs in Azure.

It supports .NET Core and the .NET Framework.

AzCopy 7.3 is used to create the backups, later version don't support table storage.

# Installation
Download and install the package from [NuGet]('https://www.nuget.org/packages/Luminis.AzureStorageBackup').
The package will install, and AzCopy 7.3 is added to the project.

# Backup
## Initialize the client

```cs
var backupAzureStorage = new Luminis.AzureStorageBackup.BackupAzureStorage(sourceAccountName, sourceAccountKey);
```

* sourceAccountName: the name of the storage account, no connectionstring or url
* sourceAccountKey: the SAS key as can be retrieved for the Azure Portal
* log (optional): an implementation of ILogger
* rootDir (optional): the folder under which AzCopy can be found. 
  * If left empty the default executable location will be used using Directory.GetCurrentDirectory()
  * When running in an Azure Function, pass the ExecutionContext.FunctionAppDirectory, which is the base folder where the Azure Function is installed

## Table storage

```cs
await backupAzureStorage.BackupAzureTablesToBlobStorage(sourceTables, destinationAccountName, destinationKey, destinationContainerName, subfolder);
```
* sourceTables: a list of string (ie: new [] { "table1", "table2" })
* destinationAccountName: the name of the storage account, no connectionstring or url
* destinationAccountKey: the SAS key as can be retrieved for the Azure Portal
* destinationContainerName: the name of the container where the backup should go to. This container is created if it does not exist yet.
* subfolder: optional folder, useful to distinct different backup-types (for example: "tables")

When backing up table storage, the files will be stored in the following directory
destinationContainerName[\subfolder]\

## Blob storage


```cs
await backupAzureStorage.BackupBlobStorage(containers, destinationAccountName, destinationKey, destinationContainerName, subfolder);
```

* sourceContainers: a list of string (ie: new [] { "container1", "container2" })
* destinationAccountName: the name of the storage account, no connectionstring or url
* destinationAccountKey: the SAS key as can be retrieved for the Azure Portal
* destinationContainerName: the name of the container where the backup should go to. This container is created if it does not exist yet.
* subfolder: optional folder, useful to distinct different backup-types (for example: "blobs")

When backing up blob storage, the files will be stored in the following directory
destinationContainerName[\subfolder]\sourceContainerName\timestamp\

# Restore
## Table storage
This cannot be done using the package. But can be done using AzCopy that is shipped with the package and can be found the azcopy foder.
<pre><code>AzCopy /Source:https://myaccount.blob.core.windows.net/mycontainer/
            /SourceKey:key1
            /Dest:https://myaccount.table.core.windows.net/mytable/ 
            /DestKey:key2 
            /Manifest:"myaccount_mytable_20140103T112020.manifest"
            /EntityOperation:InsertOrMerge</code></pre>
## Blob storage
The blob backups are nothing more then a copy of the original blobs, stored in the desired location.

To restore these, any blob storage tool can be used such as:
- [AzCopy]('https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-v10') (any version) 
- [Azure Storage explorder]('https://azure.microsoft.com/en-us/features/storage-explorer/')
- [Rest API]('https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-rest-api')

# Make changes
1. Pull code
2. Restore packages
3. Build solution

## Build and Test
Run dotnet pack to generate a nuget package

## Contribute
If you like to contribute, make a fork and submit a pull-request. 

Or get in touch.