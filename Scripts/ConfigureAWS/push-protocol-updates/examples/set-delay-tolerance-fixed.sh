#!/bin/sh

# This command will set the before/after delay tolerance values to 15000
# milliseconds.
#
# ./set-delay-tolerance.sh BUCKET
#
# Where BUCKET is the S3 bucket.
#

./list-devices.sh BUCKET | ./set-delay-tolerance.sh 15000 15000 "Updated."