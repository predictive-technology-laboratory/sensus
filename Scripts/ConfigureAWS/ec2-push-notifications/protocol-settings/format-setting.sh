#!/bin/sh

if [[ $# -ne 4 ]]
then
    echo ""
    echo "Purpose:  Formats a setting update to JSON. Does not read from standard input."
    echo ""
    echo "Usage:  ./format-setting.sh [PROPERTY TYPE] [PROPERTY NAME] [TARGET TYPE] [VALUE]"
    echo ""
    exit 1
fi

echo "{\"property-type\":\"$1\",\"property-name\":\"$2\",\"target-type\":\"$3\",\"value\":$4}"
