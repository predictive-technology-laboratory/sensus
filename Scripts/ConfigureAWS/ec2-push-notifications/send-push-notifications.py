#!/usr/bin/python

import sys
from datetime import datetime, timezone, timedelta
from urllib import parse
import hmac
import base64
import os
import signal
import atexit
import shutil
from subprocess import call
import json
import glob
from collections import namedtuple
import uuid
from urllib.request import Request, urlopen
from urllib.error import HTTPError

def main(arguments):
    # check options
    if (len(sys.argv) != 5):
        print(f"Usage: {sys.argv[0]} [s3 bucket name] [azure namespace] [azure hub] [azure key]")
        print("\t[s3 bucket name]:  S3 bucket for remote data store (e.g.: some-bucket). Do not include the s3:// prefix or trailing forward slashes.")
        print("\t[azure namespace]:  Azure push notification namespace.")
        print("\t[azure hub]:  Azure push notification hub.")
        print("\t[azure key]:  Azure push notification full access key.")
        exit()

    # we don't want this script to run more than once concurrently. check if lock file exists...
    lock_file = sys.argv[0] + ".lock"
    if (os.path.exists(lock_file)):
        print("Lock file present. Script already running.")
        return
    else:
        print("Script not running. Creating lock file.")
        lock = os.open(lock_file, os.O_CREAT | os.O_EXCL)
        os.close(lock)
        print(f"Started {datetime.now()}")
        signal.signal(signal.SIGTERM | signal.SIGINT, lambda : sys.exit(0))
        atexit.register(lambda: (os.remove(lock_file), print(f"Finished {datetime.now()}")))

    # extract arguments
    bucket = arguments[1]
    namespace = arguments[2]
    hub = arguments[3]
    key = arguments[4]
    
    # get shared access signature for communicating with azure endpoint
    url = f"https://{namespace}.servicebus.windows.net/{hub}/messages"
    sas = get_sas(url, key)

    # set up directory names
    push_notifications_dir = "push-notifications"
    requests_dir = "requests"
    tokens_dir = "tokens"
    updates_dir = "updates"

    # set up s3 paths
    s3_notifications_path = f"s3://{bucket}/{push_notifications_dir}"
    s3_requests_path = f"{s3_notifications_path}/{requests_dir}"
    #s3_tokens_path = f"{s3_notifications_path}/{tokens_dir}"
    s3_updates_path = f"{s3_notifications_path}/{updates_dir}"

    # set up local paths
    local_notifications_path = f"{bucket}-{push_notifications_dir}"
    local_requests_path = f"{local_notifications_path}/{requests_dir}"
    local_tokens_path = f"{local_notifications_path}/{tokens_dir}"
    local_updates_path = f"{local_notifications_path}/{updates_dir}"

    # create special directory for updates created by this run of the script
    # new_updates_dir = "new_updates"
    # shutil.rmtree(new_updates_dir, True)
    # os.mkdir(new_updates_dir)

    # sync notifications from s3 to local, deleting anything local that doesn't exist in s3
    print("\n************* DOWNLOADING REQUESTS FROM S3 *************")
    os.makedirs(local_notifications_path, exist_ok=True)
    call(f'aws s3 sync "{s3_notifications_path}" "{local_notifications_path}" --delete --exact-timestamps') # need the --exact-timestamps because 
                                                                                                            # the token files can be updated but 
                                                                                                            # remain the same size. without this
                                                                                                            # options such updates don't register.

    Request = namedtuple("Request", ["path", "request"])
    #Update = namedtuple("Update", ["path", "update"])

    requests = []

    for local_request_path in glob.glob(f"{local_requests_path}/*.json"):
        # if the file isn't empty, open and deserialize it
        if (os.path.getsize(local_request_path) > 0):
            with open(local_request_path) as request:
                requests.append(Request(local_request_path, json.load(request)))
        else: # delete the file if it is empty
            delete_request(local_request_path, s3_requests_path, f"Empty request {local_requests_path}. Deleting file.")

    # sort the requests by their creation time in descending order
    requests.sort(key=lambda request: request.request["creation-time"], reverse=True)

    processed_ids = {}
    updates = {}

    for request in requests:
        id = request.request["id"]
        time = datetime.fromtimestamp(request.request["time"], timezone.utc)

        # check whether we've already processed the push notification id for a request that 
        # was newer. if we haven, then the current request is obsolete and can be removed.
        if (id not in processed_ids):
            print(f"New request identifier {id} (time {time})")
            processed_ids[id] = True

            device = request.request["device"]
            device_token = get_device_token(local_tokens_path, device)

            token = device_token.get("token", "")

            # the cron scheduler runs periodically. we're going to be proactive and try to ensure that push notifications 
            # arrive at the device no later than the desired time. this is important, particularly on iOS where the arrival
            # of push notifications cancels local notifications that disrupt the user. set up a buffer accounting for the 
            # following latencies:
            #   
            #   * interval between cron runs:  5 minutes
            #   * native push notification infrastructure:  1 minute
            #
            # we don't know exactly how long it will take for the current script to make a pass through
            # the notifications. this will depend on deployment size and the protocol.

            # NOTE: the operands are transposed here compared to the original script. the original script subtracted to 
            # get a negative time_until_delivery and compared with <= to the negative value of the number of seconds in a day.
            # in this case we have a positive time_since_delivery and compare with >= to check if the request is more than a day old.
            time_horizon = datetime.now(timezone.utc) + timedelta(minutes=6)
            time_since_delivery = time_horizon - time

             # delete any push notifications that have failed for an entire day
            if (time_since_delivery >= timedelta(days=1)):
                delete_request(local_request_path, s3_requests_path, f"Push notification has failed for more than {timedelta(days=1).total_seconds():.0f} seconds. Deleting it.")
            elif (time_since_delivery < timedelta(0)): # proceed to next request if current delivery time has not arrived
                print(f"Push notification will be delivered in {time_since_delivery.total_seconds()} seconds.")
            elif (token == ""): # might not have a token (e.g., in cases where we failed to upload it or cleared it when stopping the protocol)
                delete_request(local_request_path, s3_requests_path, f"No token found. Assuming the request is stale. Deleting it.")
            elif ("update" not in request.request): # if the request does not have an update, then send it directly to the device. do not delete the request file in this case, as the app must do it to signal receipt.
                print("Pushing non-update notification")
                
                # the backend key is the file name without the extension. this value
                # is used by the app upon receipt to delete the push notification
                # request from the s3 bucket.
                backend_key = os.path.splitext(os.path.basename(os.path.normpath(request.path)))[0]

                format = request.request["format"]
                protocol = request.request["protocol"]
                title = request.request["title"]
                body = request.request["body"]
                sound = request.request["sound"]

                push_via_azure(url, sas, format, "false", id, backend_key, protocol, title, body, sound, token)
            else: # otherwise pack the update into a per-device updates file to be delivered at the end
                if (device not in updates):
                    updates[device] = []
                
                update = request.request["update"]
                update_type = update["type"]
                update_content = update["content"]

                updates[device].append({"id": id, "type": update_type, "content": update_content })

                # delete the request, as we're going to upload the updates file to s3
                delete_request(local_request_path, s3_requests_path, f"Packed request into updates file. Deleting the request.")
        else: # if the request id has been encountered already then delete the request file
            delete_request(local_request_path, s3_requests_path, f"Obsolete request identifier {id} (time {time}). Deleting file.")

    # write updates to files
    for device, device_updates in updates.items():
        device_updates_path = f"{local_updates_path}/{device}"
        
        # create device's updates directory if needed
        os.makedirs(device_updates_path, exist_ok=True)
        # write device updates to file in update directory
        with open(f"{device_updates_path}/{uuid.uuid4()}.json", "w") as update:
            json.dump(device_updates, update)

    if (len(updates) > 0):
        # sync local updates to S3 to make them available to the app. if any of the updates
        # have duplicative ids, the app will take the most recent one of each identifier.
        print("************* UPLOADING NEW UPDATES TO S3 *************")
        call(f'aws s3 sync "{local_updates_path}" "{s3_updates_path}"')

        # send push notification to each device that has a pending update
        print("************* SENDING PUSH NOTIFICATION TO EACH DEVICE WITH AN UPDATE *************")
        for device, device_updates in updates.items():
            device_token = get_device_token(local_tokens_path, device)

            token = device_token.get("token", "")

            if (token != ""):
                # the token exists. send push notification.
                format = device_token.get("format", "")
                protocol = device_token.get("protocol", "")

                push_via_azure(url, sas, format, "true", f"{uuid.uuid4()}", "", protocol, "", "", "", token)

                print(f"Pushed updates for device {device}")
            else:
                print(f"Token file does not exist for device {device}. Clearing updates for this device.")
                shutil.rmtree(f"{local_updates_path}/{device}", True)
                call(f'aws s3 rm --recursive "{s3_updates_path}/{device}"')

    return

def delete_request(local_request_path, s3_requests_path, message):
    print(message)

    call(f'aws s3 rm "{s3_requests_path}/{os.path.basename(local_request_path)}"')
    os.remove(local_request_path)

    return

def get_device_token(local_tokens_path, device):
    local_token_path = f"{local_tokens_path}/{device}.json"

    if (os.path.exists(local_token_path)):
        with open(local_token_path) as token:
            return json.load(token)

    return {} # return an empty dict if the file doesn't exist

def push_via_azure(url, sas, format, update, id, backend_key, protocol, title, body, sound, token):
    data = ""
    
    if (format == "gcm"):
        data = json.dumps({"data": {"id": id, "protocol": protocol, "backend-key": backend_key, "update": update, "title": title, "body": body, "sound": sound}})
    elif (format == "apple"):
        data = json.dumps({"id": id, "protocol": protocol, "backend-key": backend_key, "update": update, "aps": {"content-available": 1, "alert": {"title": title, "body": body}, "sound": sound}})

    request = Request(f"{url}/?direct&api-version=2015-04", bytearray(data, 'utf-8'), method="POST", headers={"ServiceBusNotification-Format": format, "ServiceBusNotification-DeviceHandle": token, "x-ms-version": "2015-04", "Authorization": sas, "Content-Type": "application/json;charset=utf-8"})
    
    try:
        with urlopen(request) as response:
            print(f"Azure push result: {response.status} {response.reason}")
    except HTTPError as error:
        print(f"Azure push result: {error.code} {error.reason}")

    return

def get_sas(url, key):
    expiration = datetime.now(timezone.utc) + timedelta(minutes = 60)
    #expiration = datetime(2020, 9, 30, 0, 0, tzinfo=timezone.utc) + timedelta(minutes = 60)
    expirationSeconds = ('%.3f' % expiration.timestamp()).replace(".000", "")

    data = f"{parse.quote_plus(url)}\n{expirationSeconds}"

    algorithm = hmac.new(key=bytearray(key, "utf-8"), msg=bytearray(data, "utf-8"), digestmod="sha256")

    signature = base64.b64encode(algorithm.digest()).decode("ascii")

    token = f"SharedAccessSignature sr={parse.quote_plus(url)}&sig={parse.quote_plus(signature)}&se={parse.quote_plus(expirationSeconds)}&skn={parse.quote_plus('DefaultFullSharedAccessSignature')}"

    return token

if (__name__ == "__main__"):
    main(sys.argv)
