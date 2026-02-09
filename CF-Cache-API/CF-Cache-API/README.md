# CF-Cache-API

Multi-tenant .NET 8 API with CloudFront caching support, deployed on AWS EC2.

## Features

- **Multi-tenant Architecture**: Isolated data per tenant (customer1, customer2, customer3)
- **Authentication**: Login system with tenant-based access
- **Employee Management**: CRUD operations with tenant isolation
- **Pagination**: 200 records per page default
- **CloudFront Ready**: Designed to work behind AWS CloudFront CDN

## Tech Stack

- .NET 8.0
- ASP.NET Core Web API
- Kestrel Web Server
- Amazon Linux 2023 (EC2)

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `GET /api/auth/users` - Get all users

### Employee Management (Requires X-Tenant-Id header)
- `GET /api/employee` - Get all employees (paginated)
- `GET /api/employee/search` - Search employees
- `GET /api/employee/by-firstname` - Search by first name
- `GET /api/employee/by-lastname` - Search by last name
- `GET /api/employee/by-company` - Search by company
- `GET /api/employee/by-position` - Search by position
- `POST /api/employee` - Add new employee

## User Credentials

| Email | Password | Tenant ID |
|-------|----------|-----------|
| a@customer1.com | abc | tenant-customer1 |
| b@customer1.com | abc | tenant-customer1 |
| c@customer1.com | abc | tenant-customer1 |
| a@customer2.com | abc | tenant-customer2 |
| b@customer2.com | abc | tenant-customer2 |
| b@customer3.com | abc | tenant-customer3 |

## Local Development

### Prerequisites
- .NET 8 SDK
- Windows/Linux/macOS

### Build
```bash
cd CF-Cache-API
dotnet build
```

### Run Locally
```bash
dotnet run
```

Application runs on: `http://localhost:5100`

### Test Locally
```bash
# Get users
curl http://localhost:5100/api/auth/users

# Login
curl -X POST http://localhost:5100/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"a@customer1.com","password":"abc"}'

# Get employees (requires tenant header)
curl http://localhost:5100/api/employee \
  -H "X-Tenant-Id: tenant-customer1"
```

## EC2 Deployment

### Prerequisites
- AWS EC2 instance (Amazon Linux 2023)
- Security Group: Allow HTTP (80) and SSH (22)
- SSH key pair (.pem file)

### Initial Setup (One-time)

1. **Publish the application**
```bash
cd CF-Cache-API
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
```

2. **Copy files to EC2**
```bash
scp -i your-key.pem -r ./publish/* ec2-user@YOUR-EC2-IP:/var/www/cf-cache-api/
```

3. **Copy deployment script**
```bash
scp -i your-key.pem ./deploy-ec2.sh ec2-user@YOUR-EC2-IP:~/
```

4. **SSH into EC2 and run deployment**
```bash
ssh -i your-key.pem ec2-user@YOUR-EC2-IP
chmod +x deploy-ec2.sh
sudo ./deploy-ec2.sh
```

5. **Configure port forwarding (if not done by script)**
```bash
# Stop nginx if running
sudo systemctl stop nginx
sudo systemctl disable nginx

# Setup port forwarding 80 -> 5100
sudo iptables -t nat -F
sudo iptables -t nat -A PREROUTING -p tcp --dport 80 -j REDIRECT --to-port 5100
sudo iptables -t nat -A OUTPUT -p tcp -d 127.0.0.1 --dport 80 -j REDIRECT --to-port 5100
```

### Update Deployment

After making code changes:

1. **Publish locally**
```bash
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
```

2. **Copy to EC2**
```bash
scp -i your-key.pem -r ./publish/* ec2-user@YOUR-EC2-IP:/var/www/cf-cache-api/
```

3. **Restart service on EC2**
```bash
ssh -i your-key.pem ec2-user@YOUR-EC2-IP
sudo systemctl restart cf-cache-api
```

### EC2 Management Commands

```bash
# Check service status
sudo systemctl status cf-cache-api

# View logs
sudo journalctl -u cf-cache-api -f

# Restart service
sudo systemctl restart cf-cache-api

# Stop service
sudo systemctl stop cf-cache-api

# Start service
sudo systemctl start cf-cache-api

# Check listening ports
sudo netstat -tlnp | grep dotnet
```

### Test EC2 Deployment

```bash
# From EC2 instance
curl http://localhost/api/auth/users

# From outside
curl http://YOUR-EC2-IP/api/auth/users
```

## CloudFront Configuration

### Origin Settings
- **Origin Domain**: Your EC2 public IP or domain
- **Origin Path**: Leave empty
- **Protocol**: HTTP only (or HTTPS if configured)

### Behavior Settings
- **Path Pattern**: `/*` (default)
- **Allowed HTTP Methods**: GET, HEAD, OPTIONS, PUT, POST, PATCH, DELETE
- **Cache Policy**: CachingOptimized or custom
- **Origin Request Policy**: AllViewer

### Headers to Forward
- `X-Tenant-Id` (required for employee endpoints)
- `Content-Type`
- `Authorization` (if implementing)

## Data Structure

### Tenants
- **tenant-customer1**: 300 employees (IDs 1-300)
- **tenant-customer2**: 300 employees (IDs 301-600)
- **tenant-customer3**: 300 employees (IDs 601-900)

Each tenant has isolated data with distinct patterns.

## Troubleshooting

### Port Already in Use
```bash
# Find process using port
netstat -ano | findstr :5100  # Windows
sudo netstat -tlnp | grep 5100  # Linux

# Kill process
taskkill /F /PID <PID>  # Windows
sudo kill -9 <PID>  # Linux
```

### Service Won't Start
```bash
# Check logs
sudo journalctl -u cf-cache-api -n 50 --no-pager

# Verify .NET runtime
dotnet --list-runtimes

# Check file permissions
ls -la /var/www/cf-cache-api/
```

### Port 80 Not Working
```bash
# Check if nginx is running
sudo systemctl status nginx

# Stop nginx if needed
sudo systemctl stop nginx
sudo systemctl disable nginx

# Verify iptables rules
sudo iptables -t nat -L -n -v
```

## Project Structure

```
CF-Cache-API/
├── Controllers/
│   ├── AuthController.cs       # Authentication endpoints
│   └── EmployeeController.cs   # Employee CRUD endpoints
├── Models/
│   ├── Employee.cs             # Employee model
│   └── User.cs                 # User model
├── Services/
│   ├── EmployeeService.cs      # Employee business logic
│   └── UserService.cs          # User authentication logic
├── appsettings.json            # Development config
├── appsettings.Production.json # Production config
├── Program.cs                  # Application entry point
├── deploy-ec2.sh              # EC2 deployment script
└── README.md                   # This file
```

## License

MIT
