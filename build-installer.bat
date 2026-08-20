@echo off
REM Builds Rogarion.Installer\bin\Release\Rogarion.Installer.msi from scratch.
REM Publishes Rogarion.App as a self-contained win-x64 executable into
REM Rogarion.Installer\staging\App\, then builds the MSI from that staging folder.
REM staging\ is git-ignored generated output, not committed.
REM
REM Run this whenever you want to cut a new installer build.

setlocal

set STAGING=%~dp0Rogarion.Installer\staging\App

echo Cleaning previous staging output...
if exist "%STAGING%" rmdir /s /q "%STAGING%"

echo.
echo Publishing Rogarion...
dotnet publish "%~dp0src\Rogarion.App\Rogarion.App.csproj" -c Release -r win-x64 --self-contained true -p:Platform=x64 -o "%STAGING%"
if errorlevel 1 goto :error

echo.
echo Copying ReadMe.txt into the install payload...
copy /y "%~dp0Rogarion.Installer\ReadMe.txt" "%STAGING%\ReadMe.txt" >nul

echo.
echo Building installer...
dotnet build "%~dp0Rogarion.Installer\Rogarion.Installer.wixproj" -c Release
if errorlevel 1 goto :error

echo.
echo Done. Installer at Rogarion.Installer\bin\Release\Rogarion.Installer.msi
exit /b 0

:error
echo.
echo Build failed.
exit /b 1
