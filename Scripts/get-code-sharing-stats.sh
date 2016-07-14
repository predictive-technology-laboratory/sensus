#!/bin/bash

androidLines=`find ../Sensus.Android -type f -iname "*.cs" ! -iname "Resource.Designer.cs" ! -iname "AssemblyInfo.cs" ! -path "*/obj/*" | xargs wc -l | tail -n 1 | sed -e 's/^[ \t]*//' | cut -d " " -f1`
iosLines=`find ../Sensus.iOS -type f -iname "*.cs" ! -path "*/obj/*" | xargs wc -l | tail -n 1 | sed -e 's/^[ \t]*//' | cut -d " " -f1`
serviceLines=`find ../SensusService -type f -iname "*.cs" | xargs wc -l | tail -n 1 | sed -e 's/^[ \t]*//' | cut -d " " -f1`
uiLines=`find ../SensusUI -type f -iname "*.cs" | xargs wc -l | tail -n 1 | sed -e 's/^[ \t]*//' | cut -d " " -f1`
totalLines=$(($androidLines + $iosLines + $serviceLines + $uiLines))

echo -n "Android ($androidLines):  "
echo "scale=3; $androidLines / $totalLines" | bc -l

echo -n "iOS ($iosLines):  "
echo "scale=3; $iosLines / $totalLines" | bc -l

echo -n "Service ($serviceLines):  "
echo "scale=3; $serviceLines / $totalLines" | bc -l

echo -n "UI ($uiLines):  "
echo "scale=3; $uiLines / $totalLines" | bc -l