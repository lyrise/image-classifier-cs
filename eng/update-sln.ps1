dotnet new sln --force -n binary-image-classifier
dotnet sln binary-image-classifier.sln add (ls -r ./src/**/*.csproj)
