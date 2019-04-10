---
uid: push_notifications
---

# Push Notifications
WARNING:  Push notifications are still in beta. It is not clear yet whether anyone but the Sensus
team will be able to configure push notifications.

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
1. Configure your AWS S3 bucket following the [guide](xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore). Note the
   name of the bucket that results from this step (e.g., test-bucket-293843-234234-23234234).

## AWS EC2
1. Configure your AWS EC2 instance using the `ec2-push-notifications/configure-ec2.sh` script within the 
   [configuration](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS.zip)
   archive. This script requires you to install the AWS CLI and the `jq` command line program. When supplying the bucket
   name, use the name obtained from the previous step.

## Sensus Protocol
1. Within your Sensus protocol, set the <xref:Sensus.Protocol.PushNotificationsHub> to the Azure Notification
Hub name, and set the <xref:Sensus.Protocol.PushNotificationsSharedAccessSignature> to
the `DefaultListenSharedAccessSignature` connection string value within the hub's Access Policy.
1. Configure the [AWS S3 remote data store](xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore) using the bucket created above.
1. Note that APNS has been observed to throttle push notification delivery rate. Please see the <xref:Sensus.Probes.PollingProbe>
   documentation for more information on polling rates and push notification throttling.

## Next Steps and Latency Expectations
If the above steps are successful, your Sensus protocol should now receive push notification support when
installed and started on participant devices. Here are some things you can do:

  * [Remote protocol updates](xref:remote_updates)
  
The Sensus team has observed that push notifications are generally delivered to iOS devices at most once 
every 5 minutes. On Android, push notifications are not reliably delivered to the device when it is locked.
Upon unlock, pending push notifications are generally delivered immediately to Sensus. If a push notification is 
delivered at a time when the device is unlocked and in use, then the notification will generally be delivered
immediately to Sensus. As noted in the documentation for <xref:Sensus.Probes.PollingProbe>s, the protocol's
design with respect to notifications should be made conservative to avoid overburdening the device and/or the 
user. The [burden tuning](xref:burden_tuning) article provides more information about push notifications and
their implications for the user and device.