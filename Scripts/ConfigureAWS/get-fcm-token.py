#!/usr/bin/python

import sys
from oauth2client.service_account import ServiceAccountCredentials

credentials = ServiceAccountCredentials.from_json_keyfile_name(sys.argv[1], ['https://www.googleapis.com/auth/firebase.messaging'])
access_token_info = credentials.get_access_token()

print access_token_info.access_token
