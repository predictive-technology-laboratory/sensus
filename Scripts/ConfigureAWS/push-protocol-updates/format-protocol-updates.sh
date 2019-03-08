#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Formats JSON update commands into full update JSON payload. Reads JSON update commands from standard input."
    echo ""
    echo "Usage:  ./format-protocol-updates.sh [MESSAGE]"
    echo ""
    exit 1
fi

echo -n "{\"updates\" : ["

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

echo "], \"user-notification\" : { \"message\" : \"$1\" } }"
