@echo off
echo Starting CF Cache API locally...

REM Set environment for development
set ASPNETCORE_ENVIRONMENT=Development
set PORT=5100

REM Run the application
dotnet run

pause