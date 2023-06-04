#! /bin/bash

echo "This script makes some assumptions about your setup (for instance, that you are using Github for hosting). You may want to manually review these changes in-case they don't work for you."

echo "Please enter your desired project name..."
read -r projectName

gitRepoName=$(git ls-remote --get-url | xargs basename -s .git)
authorUsername=$(git config user.name)

grep -rl --exclude-dir={.git,src,.github} dotnet-7-ci-cd-template . | xargs sed -i "s/dotnet-7-ci-cd-template/$gitRepoName/g"
grep -rl --exclude-dir={.git,src,.github} Placeholder . | xargs sed -i "s/Placeholder/$projectName/g"


cd src || exit
find . -depth -iname "*Placeholder*" -exec rename Placeholder $projectName '{}' \;
grep -rl Placeholder . | xargs sed -i "s/Placeholder/$projectName/g"
grep -rl hughesjs . | xargs sed -i "s/hughesjs/$authorUsername/g"

cd ../.github || exit
find . -depth -iname "*Placeholder*" -exec rename Placeholder $projectName '{}' \;
grep -rl Placeholder . | xargs sed -i "s/Placeholder/$projectName/g"

rm ../runme.sh
