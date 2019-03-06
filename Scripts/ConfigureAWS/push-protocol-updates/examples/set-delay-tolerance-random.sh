#!/bin/sh

# This command will set the before/after delay tolerance values to a random
# value with a given maximum, picking a value of 0 half of the time.
#
# ./set-delay-tolerance-random.sh BUCKET MAX
#
# Where BUCKET is the S3 bucket name and MAX is the maximum tolerance.
#
# Example:  Set the before/after delay tolerance to somewhere in [0,30] minutes:
#
# ./set-delay-tolerance-random.sh BUCKET $((1000*60*30))
# 

bucket=$1
max_delay=$2

before=0
after=0

random=$(echo "scale=4; $RANDOM / 32767" | bc)

if (( $(echo "$random >= 0.5" | bc) ))
then
    random=$(echo "scale=4; $RANDOM / 32767" | bc)
    before=$(echo "scale=2; $random * $max_delay" | bc)
    after=$before
fi

./list-devices.sh $bucket | ./set-delay-tolerance.sh $before $after "Updated delay tolerance to ${before} -- ${after}."