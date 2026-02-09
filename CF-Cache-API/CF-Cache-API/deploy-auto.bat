@echo off
echo ========================================
echo CF-Cache-API - Automated Deployment
echo ========================================
echo.

REM Configuration
set EC2_IP=YOUR-EC2-IP
set KEY_FILE=path/to/your-key.pem

echo Step 1: Publishing application...
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
if %errorlevel% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)
echo ✓ Publish successful
echo.

echo Step 2: Copying files to EC2...
scp -i %KEY_FILE% -r ./publish/* ec2-user@%EC2_IP%:/var/www/cf-cache-api/
if %errorlevel% neq 0 (
    echo ERROR: File copy failed!
    pause
    exit /b 1
)
echo ✓ Files copied
echo.

echo Step 3: Restarting service on EC2...
ssh -i %KEY_FILE% ec2-user@%EC2_IP% "sudo systemctl restart cf-cache-api && sudo systemctl status cf-cache-api"
if %errorlevel% neq 0 (
    echo ERROR: Service restart failed!
    pause
    exit /b 1
)
echo ✓ Service restarted
echo.

echo ========================================
echo Deployment Complete!
echo ========================================
echo.
echo API is now running at: http://%EC2_IP%/api/auth/users
echo.

pause
