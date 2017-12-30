#!/bin/bash
#
#  Purpose:  Prompt for path to protocol (.json) file to use when UI testing. Copies 
#            file into appropriate directory for current OS.
#

echo "Operating system:  $1"
echo "Working directory:  $(pwd)"

destinationDirectory="./Assets";
if [ "$1" == "ios" ]; then
	destinationDirectory="./Resources"
fi

echo "Contents of $destinationDirectory:"
ls -lh $destinationDirectory

read -e -p "If you would like to use a new UI testing protocol, enter its path:  " filepath

if [ "$filepath" == "" ]; then
	echo "No file selected."
else
	destinationPath="$destinationDirectory/UiTestingProtocol.json"
	echo "Copying $filepath to $destinationPath"
	cp $filepath $destinationPath

	if [ $? -eq 0 ]; then
		echo "Copied file."
	else
		echo "Failed to copy file."
	fi
fi

read -p "Press [ENTER] to continue."
