@echo off

if exist build rmdir /s /q build
mkdir build

dotnet publish "Ataraxia.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true --output build

pause
