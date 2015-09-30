#!/bin/bash
#
#  Purpose:  Prompt for path to protocol (.sensus) file to use when unit testing. Copies 
#            file into current working directory.
#

echo "Working directory:  $(pwd)"
echo
echo "Contents of Assets directory:"
ls -lh "./Assets"
echo
read -e -p "If you would like to use a new unit testing protocol, enter the path:  " filepath
echo

if [ "$filepath" == "" ]; then
	echo "No file selected."
else
	destination="./Assets/UnitTestingProtocol.sensus"

	echo "Copying $filepath to $destination"
	echo
	cp $filepath $destination

	if [ $? -eq 0 ]; then
		echo "Copied file."
	else
		echo "Failed to copy file."
	fi
fi

echo
read -p "Press [ENTER] to continue."
