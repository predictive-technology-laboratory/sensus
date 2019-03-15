#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Randomly enables or disables a probe type. Reads devices from standard input."
    echo ""
    echo "Usage:  ./enable-disable-probe-random.sh [PROBE]"
    echo ""
    echo "  [PROBE]:  Type of probe to enable/disable. Can be a base class type."
    echo ""
    echo "Example:  Randomly enable/disable all polling probes:"
    echo ""
    echo "  ./list-devices.sh BUCKET | ./enable-disable-probe-random.sh Sensus.Probes.PollingProbe"
    echo ""
    exit 1
fi

enable="false"
random=$(echo "scale=4; $RANDOM / 32767" | bc)
if (( $(echo "$random >= 0.5" | bc) ))
then
    enable="true"
fi

# create updates file
updates_file=$(mktemp)
echo -e "$(./format-protocol-update.sh Sensus.Probes.Probe Enabled $1 $enable)"\
        | ./format-protocol-updates.sh "$1 enabled:  ${enable}." > $updates_file

# push updates file to devices
cat - | ./push-updates.sh "Protocol" $updates_file "$1-enable-random"

# clean up file
rm $updates_file


