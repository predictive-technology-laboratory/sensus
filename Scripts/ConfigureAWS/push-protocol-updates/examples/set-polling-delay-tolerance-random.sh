#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Sets the before/after delay tolerance values of all polling probes"
    echo "          to a random value with a given maximum, picking a value of 0 half"
    echo "          of the time. Reads devices from standard input."
    echo ""
    echo "Usage:  set-polling-delay-tolerance-random.sh [MAX]"
    echo ""
    echo "  [MAX]:  The maximum tolerance (milliseconds)."
    echo ""
    echo "Example:  Set the before/after delay tolerance to somewhere in [0,30] minutes:"
    echo ""
    echo "  ./list-devices.sh BUCKET | ./set-polling-delay-tolerance-random.sh $((1000*60*30))"
    echo ""
    exit 1
fi

max_delay=$1

before=0
after=0

random=$(echo "scale=4; $RANDOM / 32767" | bc)

if (( $(echo "$random >= 0.5" | bc) ))
then
    random=$(echo "scale=4; $RANDOM / 32767" | bc)
    before=$(echo "($random * $max_delay + 0.5) / 1" | bc)
    after=$before
fi

cat - | ./set-polling-delay-tolerance.sh $before $after