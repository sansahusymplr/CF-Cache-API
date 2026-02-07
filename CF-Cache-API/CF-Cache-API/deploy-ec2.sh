#!/bin/bash

# EC2 Deployment Script for .NET API
# Run this script on your EC2 instance

# Update system
sudo yum update -y

# Install .NET 8 runtime
sudo yum install -y dotnet-runtime-8.0

# Create application directory
sudo mkdir -p /var/www/cf-cache-api
sudo chown ec2-user:ec2-user /var/www/cf-cache-api

# Copy application files (run this from your local machine)
# scp -r ./bin/Release/net10.0/publish/* ec2-user@your-ec2-ip:/var/www/cf-cache-api/

# Create systemd service
sudo tee /etc/systemd/system/cf-cache-api.service > /dev/null <<EOF
[Unit]
Description=CF Cache API
After=network.target

[Service]
Type=simple
User=ec2-user
WorkingDirectory=/var/www/cf-cache-api
ExecStart=/usr/bin/dotnet /var/www/cf-cache-api/CF-Cache-API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=cf-cache-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

# Setup port forwarding from 80 to 5000
sudo iptables -t nat -A PREROUTING -p tcp --dport 80 -j REDIRECT --to-port 5000
sudo iptables -t nat -A OUTPUT -p tcp --dport 80 -o lo -j REDIRECT --to-port 5000

# Save iptables rules
sudo yum install -y iptables-services
sudo service iptables save

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable cf-cache-api
sudo systemctl start cf-cache-api

# Check status
sudo systemctl status cf-cache-api

echo "Deployment complete! API should be running on port 80"
echo "Check logs with: sudo journalctl -u cf-cache-api -f"