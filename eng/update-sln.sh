#!/usr/env bash
set -euo pipefail

dotnet new sln --force -n image-classifier
dotnet sln image-classifier.sln add ./src/**/*.csproj
