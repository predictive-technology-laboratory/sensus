#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Sends new policies to survey agents. Reads devices from standard input."
    echo ""
    echo "Usage:  ./update-survey-agent-policy.sh [P]"
    echo ""
    echo "  [P]:  New delivery probability."
    echo ""
    echo "Example:  Set delivery probability to 0.7:"
    echo ""
    echo "  ./list-devices.sh BUCKET | ./update-survey-agent-policy.sh 0.7"
    echo ""
    exit 1
fi

p=$1

policy_file=$(mktemp)
echo "{\"p\":$p}" > $policy_file

# push updates file to devices
cat - | ./request-update.sh "SurveyAgentPolicy" $policy_file "survey-agent-policy"

rm $policy_file