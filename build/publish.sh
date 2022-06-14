#!/bin/sh

echo "start publish."

mkdir build/artifacts

# dotnet pack MarineLang/MarineLang.csproj -c Release -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion="${TAG_VERSION}.${COMMIT_COUNT}"
dotnet pack MarineLang/MarineLang.csproj --include-source --include-symbols -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack MarineLang.CodeAnalysis/MarineLang.CodeAnalysis.csproj --include-source --include-symbols -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack MarineLang.CodeDom/MarineLang.CodeDom.csproj --include-source --include-symbols -o build/artifacts -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg

echo "package created"

FILE_NAMES=`find build/artifacts -name "*.nupkg"`
echo "find nupkg files"
echo "${FILE_NAMES}"

for file in ${FILE_NAMES}; do
	echo "processing ${file}"
	dotnet nuget push ${file} --api-key "${API_KEY}" --source "https://api.nuget.org/v3/index.json"
done

echo "pushed"
