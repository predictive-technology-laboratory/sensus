---
uid: push_notifications
---

# Push Notifications
Sensus uses push notifications to deliver information to users and to support quality assurance
of collected data.

## Azure Notification Hub

## S3 Configuration
Create an S3 bucket that will hold the push notification requests.

## EC2 Configuration
Install the following packages
  * jq (via `sudo yum install jq`)
  * nodejs

```
curl -o- https://raw.githubusercontent.com/creationix/nvm/v0.33.8/install.sh | bash
. ~/.nvm/nvm.sh
nvm install 8.11.2
```

Set up AWS CLI credentials

Upload `get-sas.json` and `send-push-notifications.sh`

Configure cron job:

```
PATH="/home/ec2-user/.nvm/versions/node/v8.11.2/bin:/usr/local/bin:/usr/bin:/usr/local/sbin:/usr/sbin:/home/ec2-user/.local/bin:/home/ec2-user/bin"

* * * * * cd /home/ec2-user && ./send-push-notifications.sh s3://sensus-push-notification-requests
```

## Sensus Protocol