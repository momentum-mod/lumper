#!/bin/sh
# Script for testing the publish process of Lumper.
# Equivalent is performed in the CI pipeline (see build.yml).
rm -rf publish/

cd src/Lumper.UI
dotnet publish -r win-x64 -c Release

cd ../Lumper.CLI
dotnet publish -r win-x64 -c Release

cd ../../
