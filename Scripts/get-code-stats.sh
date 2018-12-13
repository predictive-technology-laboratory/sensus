#!/bin/bash

total=0
for code_dir in $(ls | grep Sensus); do
    lines=$(find $code_dir -type f -iname "*.cs" ! -iname "Resource.Designer.cs" ! -iname "AssemblyInfo.cs" ! -path "*/obj/*" | xargs wc -l | tail -n 1 | sed -e 's/^[ \t]*//' | cut -d " " -f1)
    if [ "$lines" != "" ]
    then
	total=$(($total + $lines))
	echo "$code_dir:  $lines"
    fi
done

echo "Total:  $total"
