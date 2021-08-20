#!/bin/sh

echo "start publish."

mkdir build/artifacts

# dotnet pack MarineLang/MarineLang.csproj -c Release -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion="${TAG_VERSION}.${COMMIT_COUNT}"
dotnet pack MarineLang/MarineLang.csproj --include-source --include-symbols -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg

echo "package created"

FILE_NAME=`find build/artifacts -name "*.nupkg"`
echo "find nupkg ${FILE_NAME}"

dotnet nuget push ${FILE_NAME} --api-key "${API_KEY}" --source "https://api.nuget.org/v3/index.json"

echo "pushed"
