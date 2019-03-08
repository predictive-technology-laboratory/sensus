#!/bin/sh

if [[ $# -ne 2 ]]
then
    echo ""
    echo "Purpose:  Enables or disables a probe type. Reads devices from standard input."
    echo ""
    echo "Usage:  ./disable-probe.sh [PROBE TYPE] [ENABLE]"
    echo ""
    echo "  [PROBE TYPE]:  Type of probe to enable/disable. Can be a base class type."
    echo "      [ENABLE]:  true/false"
    echo ""
    echo "Example:  Disable all polling probes:"
    echo ""
    echo "  ./list-devices.sh BUCKET | ./enable-disable-probe.sh Sensus.Probes.PollingProbe false"
    echo ""
    exit 1
fi

# create updates file
updates_file=$(mktemp)
echo -e "$(./format-protocol-update.sh Sensus.Probes.Probe Enabled $1 $2)"\
        | ./format-protocol-updates.sh "$1 enabled:  ${2}." > $updates_file

# push updates file to devices
cat - | ./push-updates.sh $updates_file "$1-enable-disable"

# clean up file
rm $updates_file


