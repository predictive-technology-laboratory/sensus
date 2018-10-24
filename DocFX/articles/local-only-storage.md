---
uid:  local_only_storage
---

# Local-Only Storage
To configure a Sensus study that stores data locally on the device rather than 
transmitting data to a <xref:Sensus.DataStores.Remote.RemoteDataStore>, proceed as follows:

1. Create a new protocol.
1. If you wish to offload data from devices via email attachment (on iOS or Android) or via Dropbox 
(Android only), then enable <xref:Sensus.Protocol.AllowLocalDataShare>. Email attachments only work
well for small samples of data. If you will be accumulating large amounts of data on an iOS device, 
then you should use iTunes file sharing instead (details below).
1. Add a local data store and disable <xref:Sensus.DataStores.Local.LocalDataStore.WriteToRemote>. This will prevent 
data from leaving the device.
1. Add a console remote data store. This is a placeholder data store that has no effect.
1. Configure the protocol probes.
1. Start protocol and run as desired.
1. Stop protocol.

### Offloading Data
After stopping the protocol, you can offload data in a few ways depending on whether the 
device is running Android or iOS and whether you have a little or a lot of data.

#### Android
Tap the protocol, select Share Local Data. Then select one of the following options.
  * For small data:  Select your email app
  * For large data:  Select Dropbox or another cloud-based file service

#### iOS
  * For small data:  Tap the protocol, select Share Local Data, and attach to an email.
  * For large data:  Connect the device to a computer via USB and open iTunes. Next, 
    select your phone icon as shown below:
    
![image](/sensus/images/itunes-device.png)

Next, use file sharing to save the folder whose name is that of the study identifier.

![image](/sensus/images/itunes-file-sharing.png)

If you do not know your study identifier, then you can view it within Sensus. To view your study
identifier, tap the protocol and select Edit. The study identifier will be shown at the top of the
screen (the long sequence of numbers, letters, and dashes).

