---
uid:  redeploying
---

# Redeploying Sensus

The Sensus license allows you to download/modify the source code and redeploy the app 
under your own branding and for your own purposes. This process will
require you to create various serices that Sensus relies on for proper operation.

## Icons

* Android:  Replace the `ic_launcher.png` icon in the `Sensus.Android/Resources/drawable` folders with your own icon.
* iOS:  Replace the `Sensus.iOS/Images.xcassets` content with your own icons.

## Packages and Licenses

The Sensus team has negotiated the open-source use of the [SfChart](https://help.syncfusion.com/wpf/sfchart/getting-started) 
library provided by Syncfusion. These files are contained [here](https://github.com/predictive-technology-laboratory/sensus/tree/develop/dependencies/Syncfusion)
and referenced in the respective Sensus.Android and Sensus.iOS applications. As this is a commercial 
library, you will need to purchase a license for this library or remove it from your app prior to deployment. This is the 
only commercial library that Sensus uses, and it is not essential to proper functioning of the app. It only provides
in-app visualization of certain data streams.

You should carefully inspect the terms and conditions associated with other libraries and packages consumed by 
Sensus to ensure that your intended use is compliant.

## Keys and IDs
Edit the following fields of `Sensus.SensusServiceHelper.cs` (in the `Sensus.Shared` project):

```
        /// <summary>
        /// App Center key for Android app. To obtain this key, create a new Xamarin Android app within the Microsoft App Center. This
        /// is optional. If you do not provide this key, then Sensus will not send Android crash reports and remote health telemetry 
        /// to the App Center.
        /// </summary>
        public const string APP_CENTER_KEY_ANDROID = "";

        /// <summary>
        /// App Center key for iOS app. To obtain this key, create a new Xamarin iOS app within the Microsoft App Center. This
        /// is optional. If you do not provide this key, then Sensus will not send iOS crash reports and remote health telemetry 
        /// to the App Center.
        /// </summary>
        public const string APP_CENTER_KEY_IOS = "";

        /// <summary>
        /// The 64-character hex-encoded string for a 256-bit symmetric AES encryption key. Used to secure protocols for distribution. Can be generated with the following command:
        /// 
        ///     openssl enc -aes-256-cbc -k secret -P -md sha1
        /// 
        /// The above was adapted from:  https://www.ibm.com/support/knowledgecenter/SSLVY3_9.7.0/com.ibm.einstall.doc/topics/t_einstall_GenerateAESkey.html
        /// 
        /// This is mandatory.
        /// </summary>
        public const string ENCRYPTION_KEY = "";

        /// <summary>
        /// The build ID, used to tag each <see cref="Datum"/>. This is an arbitrary string value, and it is optional.
        /// </summary>
        public const string BUILD_ID = "";
```

## Sensus Android

Sensus Android consumes services provided by Google Cloud Platform. Since these services and their authorized consumers are tied
to the unique fingerprint of each Android APK, you will need to create your own Google Cloud account and associated
services and then associate these servies with your app before compiling and deploying it.

1. Create a new [Google Cloud Platform](https://console.cloud.google.com) account and a new project in the account.
1. Create a new [Firebase](https://firebase.google.com/) project, add your Android app to the project, and download 
   the `google-services.json` file into the Sensus.Android project, replacing the current one.
1. Return to the Google Cloud Platform console and enable the following APIs:
   
   * Awareness API
   * Maps SDK for Android

1. View your Google Cloud Platform credentials. You should see three credentials that were auto-created (server,
   browser, and Android). Edit the Android credential and restrict the credential to your Android app's SHA-1 signature. 
   Also restrict this credential to use the following APIs:

   * Awareness API  
   * Maps SDK for Android
   * Firebase Services API

   Copy the API key for this credential and paste its value into the following fields of your `AndroidManifest.xml` file:
   
   * `com.google.android.maps.v2.API_KEY`
   * `com.google.android.awareness.API_KEY`

1. Edit the `AndroidManifest.xml` file as follows:

   * Edit the following line:

     ```
     <manifest xmlns:android="http://schemas.android.com/apk/res/android" android:installLocation="auto" package="edu.virginia.sie.ptl.sensus" android:versionName="XXXX" android:versionCode="XXXX">`
     ```    
    
     To use your package name, version name, and version code.
    
   * Edit the following values to use your app/package name.

     ```   
     <application android:label="Sensus"
     ```
     
     ```
     android:authorities="edu.virginia.sie.ptl.sensus.fileprovider"
     ```
     
     ```
     <uses-permission android:name="edu.virginia.sie.ptl.sensus.permission.MAPS_RECEIVE" />
     ```
     
     ```
     <permission android:name="edu.virginia.sie.ptl.sensus.permission.MAPS_RECEIVE" android:protectionLevel="signature" />
     ```
   
## Sensus iOS

Sensus iOS consumes services provided by Apple. Since these services and their authorized consumers are tied
to the unique certificate of each iOS app, you will need to create your own iOS developer account and associated
services and then associate these servies with your app.

1. Enable the following services within your iOS App ID:

   * HealthKit
   * Push Notifications. Create and download the development and production SSL certificates for push notifications. Keep these
     certificates in a known and secure location. You will use them to configure [push notification](xref:push_notifications) support.

1. Edit the following fields of your `Info.plist` file:
   
   * `CFBundleDisplayName`
   * `CFBundleIdentifier`
   * Each `CFBundleURLSchemes` element under `CFBundleURLTypes`
   * `CFBundleDocumentTypes`
   * Elements under `UTExportedTypeDeclarations`
   * `CFBundleShortVersionString`
   * `CFBundleVersion`
   * Each of the usage descriptions.