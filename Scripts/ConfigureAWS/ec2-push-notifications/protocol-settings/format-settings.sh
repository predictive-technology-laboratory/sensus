#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Formats JSON setting updates into a complete JSON update content payload. Reads"
    echo "          JSON setting updates from standard input."
    echo ""
    echo "Usage:  ./format-settings.sh [MESSAGE]"
    echo ""
    exit 1
fi

echo -n "{\"settings\":["

first_update=true

while read update
do
    if [ $first_update = false ]
    then
	echo -n ","
    fi

    echo -n "$update"

    first_update=false
done

echo "],\"user-notification\":{\"message\":\"$1\"}}"
