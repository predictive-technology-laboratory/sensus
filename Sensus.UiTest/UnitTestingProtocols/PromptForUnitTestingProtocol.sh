#!/bin/bash
#
#  Purpose:  Prompt for path to protocol (.sensus) file to use when unit testing. Copies 
#            file into current working directory.
#

echo "Working directory:  $(pwd)"
read -e -p "Path to protocol to use for unit testing:  " filepath
destination="./Assets/UnitTestingProtocol.sensus"
echo "Copying $filepath to $destination"
cp $filepath $destination
if [ $? -eq 0 ]; then
	read -p "Copied file. Press [ENTER]." proceed
else
	read -p "Failed to copy file. Press [ENTER]."
fi