---
uid:  local_only_storage
---

# Local-Only Storage
To configure a Sensus study that accumulates data locally on the device rather than 
transmitting to <xref:Sensus.DataStores.Remote.RemoteDataStore>, proceed as follows:

1. Create a new protocol.
1. Enable <xref:Sensus.Protocol.AllowLocalDataShare>. This will allow you to offload accumulated data.
1. Add a local data store and disable <xref:Sensus.DataStores.Local.LocalDataStore.WriteToRemote>. This will prevent 
data from leaving the device.
1. Add a console remote data store. This is a null data store that has no effect.
1. Configure probes.
1. Start protocol and run as desired.
1. Stop protocol and select Share Local Data.