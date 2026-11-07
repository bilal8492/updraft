@echo off
rem Local debug-free release build for this machine (x64). Needs the .NET 9 SDK:  winget install Microsoft.DotNet.SDK.9
dotnet build "%~dp0Updraft\Updraft.csproj" -c Release -p:Platform=x64 -r win-x64
