#!/bin/sh
echo "deleting old build result"
rm -r ./deploy
dotnet publish ./src/PinArchiveBot.Service/PinArchiveBot.Service.csproj -o ./deploy
