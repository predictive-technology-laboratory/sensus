#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Lists devices accepting push notifications. Does not read from standard input."
    echo ""
    echo "  Usage:  ./list-devices.sh [bucket]"
    echo ""
    exit 1
fi

for token_file in $(ls "$1-push-notifications")
do
    filename=$(basename -- "$token_file")
    extension="${filename##*.}"
    filename="${filename%.*}"
    if [[ $extension != "json" ]]
    then
	device_id=$(echo $filename | cut -f1 -d ":")
	protocol_id=$(echo $filename | cut -f2 -d ":")
	echo -e "$1\t$device_id\t$protocol_id"
    fi
done
