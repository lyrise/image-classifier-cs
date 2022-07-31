#!/usr/env bash
set -euo pipefail

dotnet new sln --force -n binary-image-classifier
dotnet sln binary-image-classifier.sln add ./src/**/*.csproj
