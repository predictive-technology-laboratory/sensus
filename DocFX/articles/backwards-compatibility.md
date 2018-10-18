---
uid: backwards_compat
---

# Backwards Compatibility

All platforms change over time. The developers do their best to ensure that the most recent version of the platform 
is compatible with devices running older versions of the platform; however, this cannot always be perfectly achieved. 
This page describes our approach to ensuring that Sensus development proceeds safely as the various platforms mature.

## Android
We have the following goals:

1. Provide compile-time checks on invalid use of Android APIs. For example, if we believe that Sensus is compatible 
   with version 5 and higher of the Android framework, then the build should fail if we compile against, say, version 
   4.0. If the build does not fail, then we should lower our minimum required Android API level. If the build fails 
   against Android version 4.1 or higher, then we either need to raise the minimum required API level or add backwards 
   compatible code (the latter is preferred). Ensuring that Sensus compiles at our presumed minimum compatibility level 
   is one easy way to help ensure that all compatible devices will run Sensus properly. Of course, there's no replacement
   for on-device testing.
1. Sensus should run with no errors on any version of the Android framework that is greater than or equal to the minimum. 

To achieve the above goals, we use a combination of compiler directives and runtime checks on the Android API. An example 
is the <xref:Sensus.Android.Probes.Device.AndroidScreenProbe> class, a snippet of which is shown below:


    #if __ANDROID_20__
    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        screenOn = _powerManager.IsInteractive;  // API level 20
    else
    #endif
    {
        // ignore deprecation warning
        #pragma warning disable 618
        screenOn = _powerManager.IsScreenOn;
        #pragma warning restore 618
    }

The `PowerManager.IsInteractive` property was introduced at API level 20. Thus, it cannot be referenced when compiling against 
anything less than API level 20. To satisfy goal (1) above, we build Sensus against our presumed minimum compatible API level. 
The compiler directive in the snippet above allows us to do this while at the same time making use of the newest API features 
in our deployed apps, which are built against the latest Android framework. The `Build.VERSION.SdkInt` check ensures that, 
at runtime, Sensus will only use APIs that are actually installed on the device. This achieves goal (2) above. Lastly, we treat
all compilation warnings as compilation errors within Sensus. Since the reference to `IsScreenOn` has been deprecated, this 
would normally produce a compile-time error. We use the `#pragma` statements to selectively ignore deprecation errors that we 
know should be ignored. This approach maximizes compile-time errors (giving us a chance to fix them before deployment) and
minimizes runtime errors.

## iOS
At this point, we do not have specific guidelines on backwards compatibility for iOS.