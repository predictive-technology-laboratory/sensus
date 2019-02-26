---
uid:  device_os_app
---

# Device, Operating System, and App Changes

The SensusMobile app is designed to run for extended durations on users' devices. Inevitably, users
will choose to restart their device, update their operating system (e.g. a new version of Android or iOS),
or update the SensusMobile app itself. This article sets expectations for how the app will behave in response
to these actions.

## Android

SensusMobile for Android will automatically restart in response to the above actions without any user interaction. 
When starting up, SensusMobile will resume any studies that were running prior to the above actions. This may surprise 
users, as the study start-up procedure will bring SensusMobile to the foreground potentially interrupting the user's task. 
Users should be made aware of this possibility when joining SensusMobile studies.

### iOS

SensusMobile will respond to the above actions in different ways depending on whether
[push notifications](xref:push_notifications) are enabled.

  * If push notifications are not enabled, then SensusMobile will not automatically start or issue any 
    notifications following the above actions. When the user chooses to open SensusMobile, the app will 
    resume studies that were previously running.
    
  * If push notifications are enabled, then SensusMobile will be started in the background upon receipt 
    of the first push notification that arrives after the device is restarted. Upon starting, SensusMobile
    will resume studies that were running when the device restarted.