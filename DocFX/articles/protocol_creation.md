---
uid: protocol_creation
---

# Protocol Creation
Follow the steps below to get a basic Sensus Protocol up and running.

1. [Configure](xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore) an AWS S3 bucket for data storage.
1. Within the Sensus app:
  1. Add a new Protocol.
  1. Add a [file-based local data store](xref:Sensus.DataStores.Local.FileLocalDataStore) to the Protocol.
  1. Add an [AWS S3 remote data store](xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore) to the Protocol
     based on the information provided by the S3 configuration script that you ran above.
  1. Configure the Protocol's probes.
  1. Start the Protocol.

After a period of time, specifically the [AWS S3 write delay](xref:Sensus.DataStores.Remote.RemoteDataStore.WriteDelayMS),
you should see data appear in your AWS S3 bucket. You can force data to be written to your bucket on demand by enabling 
the [write data](xref:Sensus.Protocol.AllowSubmitData) feature.