# sync the notifications directory with the AWS bucket (deletes any local that don't exist in S3)
# NOTE: assumes valid AWS credentials have already been provided.
aws s3 sync s3://sensus-push-notifications-public /home/ec2-user/sensus-notifications/notifications --delete

# get path to new notifications
NOTIFS=/home/ec2-user/sensus-notifications/notifications/*


#obtain new SAS

for n in $NOTIFS
do
        echo "processing notification file: $n"

        participant=$(jq -r '.participant' $n)

        message=$(jq -r '.message' $n)

        format=$(jq -r '.format' $n)

        # get out + verify key

        echo "sending notification to participant : $participant, format : $format, message : $message"

        # administer the notification
        #curl -v --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $participant" --header "x-ms-version: 2015-04" --header "Authorization: SharedAccessSignature sr=http%3A%2F%2Fsensus-notifications.servicebus.windows.net%2Fsensus-notifications&sig=AknDptBIH6ql%2BcJLn4W43Sjpr0ZUl1kAgy87gDp8pHo%3D&se=1525264159&skn=Test" --data '{"notification":{"body":"'"$message"'"}}' -X POST "https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages/?direct&api-version=2015-04"

        response=$(curl --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $participant" --header "x-ms-version: 2015-04" --header "Authorization: <removed for security>" --data '{"notification":{"body":"'"$message"'"}}' -X POST "https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages/?direct&api-version=2015-04" --write-out %{http_code} --silent --output /dev/null servername)

        echo "$response"
        if [[ $response -eq 201000  ]] # strcmp these, find out why resp 6 dighec
        then
                echo "success!"
                # On success, delete the notification locally
                rm "$n"
        else
                echo "failure!"
        fi

done

# re-sync with remote S3 notifs (mirror image of initial sync)
aws s3 sync /home/ec2-user/sensus-notifications/notifications s3://sensus-push-notifications-public --delete