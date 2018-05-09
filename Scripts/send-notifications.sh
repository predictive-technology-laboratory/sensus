# sync the notifications directory with the AWS bucket (deletes any local that don't exist in S3)
aws s3 sync s3://sensus-push-notifications-public /home/ec2-user/sensus-notifications/notifications --delete

# count number of new notifications in dir, subtract dot/dotdot
nncount=$(ls -f /home/ec2-user/sensus-notifications/notifications/ | wc -l)
nncount=$(expr $nncount - 2)

# if there are new notifications
if [ "$nncount" -gt 0 ]
then
        echo "SNS detected $nncount new notifications."

        # get path to new notifications
        NOTIFS=/home/ec2-user/sensus-notifications/notifications/*

        # refresh SAS for communication with azure(lasts one minute)
        sas=$(node refresh-sas.js)

        for n in $NOTIFS
        do
                #echo "processing notification file: $n"

                participant=$(jq -r '.participant' $n)

                message=$(jq -r '.message' $n)

                format=$(jq -r '.format' $n)

                sendtime=$(jq -r '.time' $n)

                snskey=$(jq -r '.key' $n)

                echo -e "attempting to send notification..\n\nparticipant : $participant \nformat : $format \nmessage : $message \n\n"

				# SNS authorization step (details removed for security)
                if [<snskey is valid>]
                then
                        # detect if the time is in a valid send window
                        # (any time before ten seconds from now and after expiry (tbd))
                        timesecs=$(date +%s%3N)
                        timewindow=$(expr $timesecs + 10000)

                        if [ $sendtime -le $timewindow ]
                        then
                                response=$(curl --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $participant" --header "x-ms-version: 2015-04" --header "Authorization: $sas" --data '{"notification":{"body":"'"$message"'"}}' -X POST "https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages/?direct&api-version=2015-04" --write-out %{http_code} --silent --output /dev/null servername)

                                if [[ $response -eq 201000  ]]
                                then
                                        echo "201- notification created in Hub."
                                        # On success, delete the notification locally
                                        rm "$n"
                                else
                                        echo "Error creating notification, retrying on next cycle..."
                                fi
                        fi
                else
                        echo "SNS Error: invalid SNS key."
                fi
        done
        # re-sync with remote S3 notifs (mirror image of initial sync)
        aws s3 sync /home/ec2-user/sensus-notifications/notifications s3://sensus-push-notifications-public --delete
else
        echo "no new notifications detected"
fi
