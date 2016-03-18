#!/bin/sh

if [ $# -ne 7 ]; then
    echo "Purpose:  Creates a release of Sensus for Android and iOS, based on the current GitHub branch."
    echo ""
    echo "Usage:  ./ReleaseSensus.sh [version] [android keystore path] [android keystore password] [github prerelease] [encryption key] [xamarin insights key] [google play track]"
    echo "\t[version]:  Version name, following semantic versioning guidelines (e.g., 0.3.1-prerelease)."
    echo "\t[android keystore path]:  Path to Android keystore file."
    echo "\t[android keystore password]:  Password used to open the Android keystore and signing key (assumed to be the same)."
    echo "\t[github prerelease]:  Whether or not the GitHub release should be marked as a prerelease (true/false)."
    echo "\t[encryption key]:  Encryption key for Sensus data. If this is changed, the new release of Sensus will be unable to work with any data encrypted with previous versions of Sensus."
    echo "\t[xamarin insights key]:  API key for Xamarin Insights."
    echo "\t[google play track]:  Google Play track (alpha, beta, production, or rollout)."
    echo ""
    echo "For example (for a prerelease to beta):  ./ReleaseSensus.sh 0.8.0-prerelease /path/to/sensus.keystore keystore_password true 234-23-4-23f-sdf-4 23423423-42342-34-24 beta"
    exit 1
fi

#######################
##### PREPARATION #####
#######################

# get name of release branch -- this is the current branch
releaseBranch=$(git rev-parse --abbrev-ref HEAD)

# grab latest commit on the release branch
git pull

##########################
##### GITHUB RELEASE #####
##########################

# reset encryption key, since we don't want it to get committed into the repository
sed -i '' "s/private const string ENCRYPTION_KEY = \"$5\"/private const string ENCRYPTION_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs

# reset Xamarin Insights key, since we don't want it to get committed to the repository
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$6\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs

# show updates that will be committed to the repository
echo "The following differences will be committed to the repository for release."
git difftool

# commit, push to github, merge the release branch into master, and push master to github
git commit -a -m "Sensus release v$1."
git push
git checkout master
git merge $releaseBranch
git push

# if we're not releasing from develop, then any changes we just made to the release branch need to be merged into develop.
if [ "$releaseBranch" != "develop" ]; then
    git checkout develop
    git merge $releaseBranch
    git push
    git checkout master
fi

# create tag for release and push tag to repository
tag_name="Sensus-v$1"
git tag -a $tag_name -m "Tag for Sensus release v$1."
git push origin $tag_name

# draft github release based on new tag
curl -u MatthewGerber --data "{\"tag_name\": \"$tag_name\",\"target_commitish\": \"master\",\"name\": \"Sensus release v$1\",\"body\": \"Release of Sensus version $1.\",\"draft\": false,\"prerelease\": $4}" https://api.github.com/repos/predictive-technology-laboratory/sensus/releases

# switch back to release branch
git checkout $releaseBranch
