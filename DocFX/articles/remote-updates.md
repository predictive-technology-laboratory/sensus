---
uid: remote_updates
---

# Remote Updates

After configuring [push notifications](xref:push_notifications), it is possible
to send protocol update commands to devices. This article describes the updates
that are directly supported:

  * Polling delay tolerance:  Use the following command within the home directory of your 
  push notification backend to update the <xref:Sensus.Probes.PollingProbe.DelayToleranceBeforeMS> and
  <xref:Sensus.Probes.PollingProbe.DelayToleranceAfterMS> values:
  
  ```
  ./list-devices.sh BUCKET | ./set-polling-delay-tolerance.sh BEFORE AFTER MESSAGE
  ```
  
  where `BUCKET` is the S3 bucket name, `BEFORE` is the new value for <xref:Sensus.Probes.PollingProbe.DelayToleranceBeforeMS>,
  `AFTER` is the new value for <xref:Sensus.Probes.PollingProbe.DelayToleranceAfterMS>, and `MESSAGE`
  is a message to display to the user to notify them of the update.
  
  * Probe enable/disable:  Use the following command within the home directory of your
  push notification backend to enable/disable a <xref:Sensus.Probes.Probe>:
  
  ```
  ./list-devices.sh BUCKET | ./enable-disable-probe.sh PROBE ENABLE
  ```
  
  where `BUCKET` is the S3 bucket name, `PROBE` is the full type name of the <xref:Sensus.Probes.Probe>
  to enable/disable, and `ENABLE` is either `true` or `false`.
  
The above commands are the only ones that are directly supported; however, the remote update 
capability is entirely generalized. See the [scripts](https://github.com/predictive-technology-laboratory/sensus/tree/develop/Scripts/ConfigureAWS/push-protocol-updates)
for how to extend the above set of commands to update any settings of interest.