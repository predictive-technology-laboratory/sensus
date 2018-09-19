---
uid: push_notifications
---

# Push Notifications

## Introduction
Sensus uses push notifications to deliver information to users, to let Sensus know when
updated information is available, and to generally support quality assurance of collected 
data. This article describes how to enable push notifications within your Sensus deployment.
The primary components are as follows:

  * Firebase Cloud Messaging:  This is the Android push notification service.
  * Apple Push Notification Service:  This is the iOS push notification service.
  * Azure Notification Hub:  This is the central manager and distributor of push notifications.
                             This component takes care of distributing push notifications to the
                             platform-specific (Android and iOS) push notification services.
  * AWS S3:  This is the backend in which push notifications will be stored for future delivery.
  * AWS EC2:  This is the backend processor that will read push notifications from AWS S3 and
              deliver them to the Azure Notification Hub.
  * Sensus Protocol:  This contains settings that let the app know where to register for push 
                      notifications.

The following sections explain the configuration of each component.

## Firebase Cloud Messaging
Configure Firebase Cloud Messaging following the official Google documentation.

## Apple Push Notification Service
Configure the Apple Push Notification Service following the official Apple documentation.

## Azure Notification Hub
1. Create a new Notification Hub within the Azure portal.
1. Within the Apple (APNS) settings, upload the the APNS certificate generated from 
   the previous steps. Set the Application Mode to Production.
1. Within the Google (GCM) settings, enter the API Key that you generated from the
   previous steps.
1. Under Manage -> Access Policies, make a note of the `DefaultListenSharedAccessSignature` and
   `DefaultFullSharedAccessSignature`. These names are similar, so be careful not to confuse them
   in the steps that follow.

## AWS S3
1. Configure your AWS S3 bucket following the [guide](xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore).

## AWS EC2
1. Configure your AWS EC2 instance using the `configure-ec2.sh` script within the [configuration](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS.zip)
   archive. This script requires you to install the AWS CLI and the `jq` command line program.

## Sensus Protocol
1. Within your Sensus protocol, set the [Push Notifications Hub](xref:Sensus.Protocol.PushNotificationsHub) to the Azure Notification
Hub name, and set the [Push Notifications Shared Access Signature](xref:Sensus.Protocol.PushNotificationsSharedAccessSignature) to
the `DefaultFullSharedAccessSignature` of the Azure Notification Hub.
1. Configure the [AWS S3 remote data store](xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore) using the bucket created above.

## Conclusion
If the above steps are successful, your Sensus protocol should now receive push notification support when
installed and started on participant devices.

# FCM-only

1. Add Firebase project. Download the `GoogleServices-Info.plist` file that is created and replace the existing
   one within the Sensus.iOS project.
1. Add iOS app to your Firebase project, uploading the development and production APN certificates.
1. Secure the API KEY listed in the `GoogleServices-Info.plist` file, making it specific to your iOS bundle ID and permitting FCM.
1. Project settings -> Service accounts -> Generate new private key (keep safe).
1. pip install --upgrade google-api-python-client (should be at least version 4.1.3).
1. Use cases for FCM (redeployment required) and Azure (use as-is).
