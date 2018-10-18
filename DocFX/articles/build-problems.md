---
uid: build_problems
---

# Common Problems

## Android

### INSTALL_FAILED_UPDATE_INCOMPATIBLE
If you develop on multiple machines (e.g., laptop, desktop, etc.) or if you switch from a source code build/deploy 
to the Google Play Store app (or vice versa), you may run into the following deployment error:

    Deployment failed because of an internal error: Failure [INSTALL_FAILED_UPDATE_INCOMPATIBLE]

This means that the currently installed Sensus is not signed by the same authority as the version you are trying to 
deploy. The solution is simple:

    adb uninstall edu.virginia.sie.ptl.sensus

This will uninstall the Sensus app from your device, deleting all data associated with the application. After 
uninstalling, you should be able to deploy your app to your device.