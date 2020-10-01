#!/bin/sh

echo "start publish."

TAG_VERSION=`git describe --exact-match`

if [ ${?} = "128" ]; then
  echo "tag not found."
  exit 0
fi

echo "release version ${TAG_VERSION}"

COMMIT_HASH=`git rev-parse --short HEAD`

# dotnet pack MarineLang/MarineLang.csproj -c Release --include-source --include-symbols -o build/artifacts -p:PackageVersion="${TAG_VERSION}+${COMMIT_HASH}"
dotnet pack MarineLang/MarineLang.csproj --include-source --include-symbols -o build/artifacts -p:PackageVersion="${TAG_VERSION}+${COMMIT_HASH}"

echo "package created"

dotnet nuget push "build/artifacts/MarineLang.${TAG_VERSION}.nupkg" -s https://api.nuget.org/v3/index.json -k $API_KEY

echo "pushed"
