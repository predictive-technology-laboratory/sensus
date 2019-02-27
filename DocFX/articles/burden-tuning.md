---
uid: burden_tuning
---

# Burden Tuning

Burden is a key consideration when deploying Sensus to users. This article addresses key dimensions of
burden placed on the user and the device and offers advice for tuning the balance of burden and data
collection.

## [Data polling](xref:Sensus.Probes.PollingProbe)

Polling-style probes within Sensus run on schedules. Sensus will attempt to poll for data according to these
schedules. Each polling operation consumes power, the actual amount of which depends on the hardware involved. 
For example, the <xref:Sensus.Probes.Location.PollingLocationProbe> uses the device's GPS chip, which consumes 
significant power. Regardless of the probe and hardware involved, the following parameters govern polling
behavior and associated power consumption.

  * <xref:Sensus.Probes.PollingProbe.PollingSleepDurationMS>:  This parameter determines how frequently the
  probe attempts to poll for data.
  
  * <xref:Sensus.Probes.PollingProbe.DelayToleranceAfterMS> and <xref:Sensus.Probes.PollingProbe.DelayToleranceAfterMS>:
  Each polling probe runs on its own schedule independent of the other probes' schedules. As a result, the overall polling
  timeline will become increasingly crowded as additional polling probes are enabled. The operating system will
  therefore have fewer opportunities to turn off the CPU and conserve power. To address this, Sensus allows you to 
  configure tolerance within the polling schedule using these two parameters. If a polling probe is scheduled to poll
  for data at time `t`, these two parameters will create a window around `t` within which to search for other polling
  events that are already scheduled for other probes. If another polling operation is already scheduled within this 
  window, then the new polling operation will be batched with the existing operation and will be executed at the
  same time. Widening the scheduling tolerance around `t` will increase the prevalence of batching, decrease
  crowding of the overall polling timeline, and increase the amount of time when CPU is allowed to sleep.
  
  * Android listening
  
## Data Transfer

## Notifications