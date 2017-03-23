#!/bin/sh

# reset encryption key, since we don't want it to get committed into the repository
sed -i '' "s/public const string ENCRYPTION_KEY = \"$5\"/public const string ENCRYPTION_KEY = \"\"/g" ../../Sensus.Shared/SensusServiceHelper.cs

# reset Xamarin Insights key, since we don't want it to get committed to the repository
sed -i '' "s/public const string XAMARIN_INSIGHTS_APP_KEY = \"$6\"/public const string XAMARIN_INSIGHTS_APP_KEY = \"\"/g" ../../Sensus.Shared/SensusServiceHelper.cs