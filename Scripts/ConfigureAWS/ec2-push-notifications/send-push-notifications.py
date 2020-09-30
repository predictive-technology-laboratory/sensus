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
    sas = get_sas(namespace, hub, key)

    # set up directory names
    push_notifications_dir = "push-notifications"
    requests_dir = "requests"
    tokens_dir = "tokens"
    updates_dir = "updates"

    # set up s3 paths
    s3_notifications_path = f"s3://{bucket}/{push_notifications_dir}"
    s3_requests_path = f"{s3_notifications_path}/{requests_dir}"
    s3_tokens_path = f"{s3_notifications_path}/{tokens_dir}"
    s3_updates_path = f"{s3_notifications_path}/{updates_dir}"

    # set up local paths
    local_notifications_path = f"{bucket}-{push_notifications_dir}"
    local_requests_path = f"{local_notifications_path}/{requests_dir}"
    local_tokens_path = f"{local_notifications_path}/{tokens_dir}"
    local_updates_path = f"{local_notifications_path}/{updates_dir}"

    # create special directory for updates created by this run of the script
    new_updates_dir = "new_updates"
    shutil.rmtree(new_updates_dir, True)
    os.mkdir(new_updates_dir)

    # sync notifications from s3 to local, deleting anything local that doesn't exist in s3
    print("\n************* DOWNLOADING REQUESTS FROM S3 *************")
    os.makedirs(local_notifications_path, exist_ok=True)
    call(f"aws s3 sync '{s3_notifications_path}' '{local_notifications_path}' --delete --exact-timestamps") # need the --exact-timestamps because 
                                                                                                            # the token files can be updated but 
                                                                                                            # remain the same size. without this
                                                                                                            # options such updates don't register.



def get_sas(namespace, hub, key):
    url = f"https://{namespace}.servicebus.windows.net/{hub}/messages"

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
