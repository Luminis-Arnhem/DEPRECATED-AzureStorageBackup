# Introduction 
This is the repository that contains AzureStorageBackup functionality.

# Getting Started
1. Pull code
2. Restore packages
3. Build solution

# Build and Test
Run dotnet pack to generate a nuget package

# How to restore table storage
    AzCopy /Source:https://myaccount.blob.core.windows.net/mycontainer/
            /SourceKey:key1
            /Dest:https://myaccount.table.core.windows.net/mytable/ 
            /DestKey:key2 
            /Manifest:"myaccount_mytable_20140103T112020.manifest"
            /EntityOperation:InsertOrMerge

