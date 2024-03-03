Remove-Item ./pub/win-x64/* -Recurse

$Env:NativeDepsPlatform="Windows"
$Env:PlatformTarget="x64"

dotnet publish ./src/ImageClassifier -p:PublishSingleFile=true --runtime win-x64 --configuration Release --self-contained true --output ./pub/win-x64/
