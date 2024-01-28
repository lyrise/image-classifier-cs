dotnet new sln --force -n image-classifier
dotnet sln image-classifier.sln add (ls -r ./src/**/*.csproj)
