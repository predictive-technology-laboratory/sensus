#!/bin/sh

# reset encryption key, since we don't want it to get committed into the repository
sed -i '' "s/private const string ENCRYPTION_KEY = \"$5\"/private const string ENCRYPTION_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs

# reset Xamarin Insights key, since we don't want it to get committed to the repository
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$6\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs