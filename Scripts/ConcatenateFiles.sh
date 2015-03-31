#!/bin/sh

if [ $# -ne 2 ]; then
    echo "Usage:  ./ConcatenateFiles.sh [directory] [file]"
    echo "\t[directory]:  Directory containing files to concatenate"
    echo "\t[file]:  File to create"
    echo ""
    echo "For example:  ./ConcatenateFiles.sh data all_data"
    exit 1
fi

rm -f $2

find $1 -type f -exec sh -c "cat {} >> $2" \;