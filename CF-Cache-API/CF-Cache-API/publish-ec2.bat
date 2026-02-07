@echo off
echo Publishing CF Cache API for EC2 deployment...

REM Clean previous builds
dotnet clean

REM Publish for Linux x64 (EC2)
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

echo.
echo Published to ./publish directory
echo.
echo To deploy to EC2:
echo 1. Copy ./publish/* to your EC2 instance: /var/www/cf-cache-api/
echo 2. Run the deploy-ec2.sh script on your EC2 instance
echo.
echo Example SCP command:
echo scp -r ./publish/* ec2-user@YOUR-EC2-IP:/var/www/cf-cache-api/

pause