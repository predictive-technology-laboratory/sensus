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
1. Set up an EC2 instance (a `t2.micro` free instance should be fine).
1. SSH into the instance.
1. Install the following packages
  * jq (via `sudo yum install jq`)
  * nodejs

```
curl -o- https://raw.githubusercontent.com/creationix/nvm/v0.33.8/install.sh | bash
. ~/.nvm/nvm.sh
nvm install 8.11.2
```

1. Install an AWS credentials profile in the EC2 instance that has read/write access to the AWS S3 bucket configured above.
1. Upload [`get-sas.json`](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS/get-sas.js) and 
   [`send-push-notifications.sh`](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS/send-push-notifications.sh)
   to the home directory of the EC2 instance.
1. Edit `get-sas.json` to include the `url` (use the name of your Azure Notification Hub) and `sharedAccessKey` (use the 
   value of `DefaultListenSharedAccessSignature`, as copied from the above steps).
1. Configure a cron job to periodically monitor your S3 bucket for push notifications and deliver those whose time has arrived. 
   Use the `crontab -e` command to edit the cron schedule and include the following in the file:

```
PATH="/home/ec2-user/.nvm/versions/node/v8.11.2/bin:/usr/local/bin:/usr/bin:/usr/local/sbin:/usr/sbin:/home/ec2-user/.local/bin:/home/ec2-user/bin"

* * * * * cd /home/ec2-user && ./send-push-notifications.sh XXXX
```

where `XXXX` is the name of the S3 bucket you configured above (without the `s3://` prefix).

## Sensus Protocol
Within your Sensus protocol, set the [Push Notifications Hub](xref:Sensus.Protocol.PushNotificationsHub) to the Azure Notification
Hub name, and set the [Push Notifications Shared Access Signature](xref:Sensus.Protocol.PushNotificationsSharedAccessSignature) to
the `DefaultFullSharedAccessSignature` of the Azure Notification Hub.

## Conclusion
If the above steps are successful, your Sensus protocol should now receive push notification support when
installed and started on participant devices.