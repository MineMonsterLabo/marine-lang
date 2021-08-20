#!/bin/sh

echo "start publish."

# TAG_VERSION=`git describe --exact-match`
# TAG_VERSION="0.1.0-alpha"

# if [ ${?} = "128" ]; then
#   echo "tag not found."
#   exit 0
# fi

# echo "release version ${TAG_VERSION}"

# COMMIT_HASH=`git rev-parse --short HEAD`
COMMIT_COUNT=`git log --oneline | wc -l`

# echo "release version hash ${COMMIT_HASH}"
echo "release version hash ${COMMIT_COUNT}"

# dotnet pack MarineLang/MarineLang.csproj -c Release -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion="${TAG_VERSION}.${COMMIT_COUNT}"
# dotnet pack MarineLang/MarineLang.csproj --include-source --include-symbols -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion="${TAG_VERSION}.${COMMIT_COUNT}"
nuget pack MarineLang/MarineLang.csproj -IncludeReferencedProjects -Symbols -SymbolPackageFormat snupkg -OutputFileNamesWithoutVersion

echo "package created"

dotnet nuget push "MarineLang.nupkg" --api-key "${API_KEY}" --source "https://api.nuget.org/v3/index.json"

echo "pushed"
