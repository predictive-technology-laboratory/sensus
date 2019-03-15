#!/bin/sh

if [[ $# -ne 4 ]]
then
    echo ""
    echo "Purpose:  Formats an update command to JSON. Does not read from standard input."
    echo ""
    echo "Usage:  ./format-protocol-update.sh [PROPERTY TYPE] [PROPERTY NAME] [TARGET TYPE] [VALUE]"
    echo ""
    exit 1
fi

echo "{\"property-type\":\"$1\",\"property-name\":\"$2\",\"target-type\":\"$3\",\"value\":\"$4\"}"
