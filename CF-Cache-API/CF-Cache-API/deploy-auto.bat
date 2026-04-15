@echo off
echo ========================================
echo CF-Cache-API - Automated Deployment
echo ========================================
echo.

REM Configuration
set ORIGIN1_HOST=ec2-18-119-100-53.us-east-2.compute.amazonaws.com
set ORIGIN2_HOST=ec2-16-146-108-64.us-west-2.compute.amazonaws.com
set KEY1=C:\Users\sansahu\Downloads\sansahu-pdm-poc-payer-migration.pem
set KEY2=C:\Users\sansahu\Downloads\sansahu-pdm-poc-payer-migration-origin2.pem

echo Step 1: Publishing application...
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
if %errorlevel% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)
echo ✓ Publish successful
echo.

echo Step 2: Copying files to Origin 1 (us-east-2)...
scp -i %KEY1% -r ./publish/* ec2-user@%ORIGIN1_HOST%:/var/www/cf-cache-api/
if %errorlevel% neq 0 (
    echo ERROR: Origin 1 file copy failed!
    pause
    exit /b 1
)
echo ✓ Origin 1 files copied
echo.

echo Step 3: Copying files to Origin 2 (us-west-2)...
scp -i %KEY2% -r ./publish/* ec2-user@%ORIGIN2_HOST%:/var/www/cf-cache-api/
if %errorlevel% neq 0 (
    echo ERROR: Origin 2 file copy failed!
    pause
    exit /b 1
)
echo ✓ Origin 2 files copied
echo.

echo Step 4: Restarting service on Origin 1...
ssh -i %KEY1% ec2-user@%ORIGIN1_HOST% "sudo systemctl restart cf-cache-api && sudo systemctl status cf-cache-api"
if %errorlevel% neq 0 (
    echo ERROR: Origin 1 restart failed!
    pause
    exit /b 1
)
echo ✓ Origin 1 restarted
echo.

echo Step 5: Restarting service on Origin 2...
ssh -i %KEY2% ec2-user@%ORIGIN2_HOST% "sudo systemctl restart cf-cache-api && sudo systemctl status cf-cache-api"
if %errorlevel% neq 0 (
    echo ERROR: Origin 2 restart failed!
    pause
    exit /b 1
)
echo ✓ Origin 2 restarted
echo.

echo ========================================
echo Deployment Complete!
echo ========================================
echo.
echo Origin 1: http://18.119.100.53/api/health
echo Origin 2: http://16.146.108.64/api/health
echo.

pause
