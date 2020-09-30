#!/bin/sh

echo "start publish."

TAG_VERSION=`git describe --abbrev=0 --tags`

if [ ${?} = "128" ]; then
  echo "tag not found."
  exit 0
fi

# dotnet pack MarineLang/MarineLang.csproj -c Release --include-source --include-symbols -o build/artifacts -p:PackageVersion=$TAG_VERSION
dotnet pack MarineLang/MarineLang.csproj --include-source --include-symbols -o build/artifacts -p:PackageVersion=$TAG_VERSION

dotnet nuget push "build/artifacts/MarineLang.${TAG_VERSION}.nupkg" -s https://api.nuget.org/v3/index.json -k $API_KEY