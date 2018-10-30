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
Tap the protocol and select Share Local Data. You'll first be asked whether you would like to share
using USB. This option will allow you to offload data to a computer over a USB cable. You will need
to install the [Android File Transfer](https://www.android.com/filetransfer/) app on your computer
in order to access the data. This option works well for large and small data. If you choose not to 
use USB, then you will be asked to select an app (e.g., Dropbox or GDrive) to share the data.

#### iOS
Sharing options are more limited on iOS compared to Android.
  * For small data:  Tap the protocol, select Share Local Data, and attach to an email.
  * For large data:  Connect the device to a computer via USB and open iTunes. Next, 
    select your phone icon as shown below:
    
![image](/sensus/images/itunes-device.png)

Next, use file sharing to save the folder whose name is that of the study identifier.

![image](/sensus/images/itunes-file-sharing.png)

If you do not know your study identifier, then you can view it within Sensus. To view your study
identifier, tap the protocol and select Edit. The study identifier will be shown at the top of the
screen (the long sequence of numbers, letters, and dashes).