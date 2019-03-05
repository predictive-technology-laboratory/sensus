---
uid: burden_tuning
---

# Burden Tuning

Burden is a key consideration when deploying Sensus to users. This article addresses key dimensions of
burden placed on the user and the user's device. It also offers advice for tuning the balance of burden 
and data collection.

## [Data Polling](xref:Sensus.Probes.PollingProbe)

Polling-style probes within Sensus run on schedules. Sensus will attempt to poll for data according to these
schedules. Each polling operation consumes power, the effective amount of which depends on the hardware involved. 
For example, the <xref:Sensus.Probes.Location.PollingLocationProbe> uses the device's GPS chip, which consumes 
significant power. Contrast this with the <xref:Sensus.Probes.Movement.AccelerometerProbe>, which uses the device's 
accelerometer chip -- a relatively low-power component. Regardless of the <xref:Sensus.Probes.Probe> and hardware 
involved, the following parameters govern polling behavior and associated power consumption.

  * <xref:Sensus.Probes.PollingProbe.PollingSleepDurationMS>:  This parameter determines how frequently the
  <xref:Sensus.Probes.PollingProbe> attempts to poll for data.
  
  * <xref:Sensus.Probes.PollingProbe.DelayToleranceAfterMS> and <xref:Sensus.Probes.PollingProbe.DelayToleranceAfterMS>:
  Each polling probe runs on its own schedule independent of the other probes' schedules. As a result, the overall polling
  timeline will become increasingly crowded as additional polling probes are enabled. The operating system will
  therefore have fewer opportunities to turn off the CPU and conserve power. To address this, Sensus allows you to 
  configure tolerance within the polling schedule using these two parameters. If a polling probe is scheduled to poll
  for data at time `t`, these two parameters will create a window around `t` within which to search for other polling
  events that are already scheduled for other probes. If another polling operation is already scheduled within this 
  window, then the new polling operation will be batched with the existing operation and will be executed at approximately 
  the same time. Widening the scheduling tolerance around `t` will increase the prevalence of batching, decrease
  crowding of the overall polling timeline, and increase the amount of time when CPU is allowed to sleep.
  
  * <xref:Sensus.Probes.ListeningProbe.KeepDeviceAwake> (Android only):  This parameter determines whether Sensus 
  will hold a [partial wake lock](https://developer.android.com/reference/android/os/PowerManager.html#PARTIAL_WAKE_LOCK)
  on the CPU when running a <xref:Sensus.Probes.ListeningProbe>. If this parameter is enabled for any such probe, then
  all probes regardless of type will collect data unabated, as the CPU will be powered on despite the display being
  off.
  
## Data Transfer

It is expensive, both in terms of power and data network usage, to transfer collected data from the device to a 
<xref:Sensus.DataStores.Remote.RemoteDataStore>. The following parameters govern transfer behavior and associated
power and network usage:

  * <xref:Sensus.DataStores.Remote.RemoteDataStore.WriteDelayMS>:  This parameter determines how frequently
  Sensus writes data from the device to the <xref:Sensus.DataStores.Remote.RemoteDataStore>. After writing
  data successfully, the data are deleted and subsequent writes operate only on data subsequently collected.
  The power and network usage of these writes will be determined by the number, type, and configuration of
  probes that are enabled.
  
  * <xref:Sensus.DataStores.Remote.RemoteDataStore.RequireWiFi>:  This parameter determines whether WiFi
  is required for data transfer.
  
  * <xref:Sensus.DataStores.Remote.RemoteDataStore.RequireCharging>:  This parameter determines whether
  external power is required for data transfer.
  
  * <xref:Sensus.DataStores.Remote.RemoteDataStore.RequiredBatteryChargeLevelPercent>:  If external power
  is not required, then this parameter determines a minimum charge level required for data transfer.

## Notifications

Sensus will issue notifications for various states of the app. Whether a notification is issued for each app state
is partially configurable depending on the <xref:Sensus.Probes.Probe> and operating system.

  * <xref:Sensus.Probes.PollingProbe.AlertUserWhenBackgrounded> (iOS only):  Each 
  <xref:Sensus.Probes.PollingProbe> in iOS is capable of issuing a notification to the user when the 
  polling operation is scheduled to occur. This parameter determines whether these notifications are issued.