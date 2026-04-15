# CF-Cache-API

A .NET 8 multi-tenant REST API with CloudFront caching and Lambda@Edge authentication.

## Architecture

- **CF-Cache-API** — ASP.NET Core Web API (origin server behind CloudFront)
- **lambda-edge-auth** — Lambda@Edge function that validates `TenantCtx` cookies and injects `X-Tenant-Id` / `X-Entity` headers into origin requests

### Auth Flow

1. User logs in via `POST /api/auth/upsert/login` → API mints an HMAC-signed `TenantCtx` cookie
2. Subsequent requests pass through CloudFront → Lambda@Edge validates the cookie signature and expiry
3. On success, Lambda@Edge injects `X-Tenant-Id` and `X-Entity` headers before forwarding to origin
4. On mutation (update/delete), the API triggers CloudFront cache invalidation

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/upsert/login` | Authenticate and set TenantCtx cookie |
| POST | `/api/auth/upsert/logout` | Clear TenantCtx cookie |
| GET | `/api/auth/search/users` | List all users |
| GET | `/api/employee/search/{tenantId}` | List employees (paginated, entity-filtered) |
| GET | `/api/employee/search/{tenantId}/{id}` | Get employee by ID |
| GET | `/api/employee/search/{tenantId}/search` | Search employees by multiple fields |
| GET | `/api/employee/search/{tenantId}/by-firstname` | Search by first name |
| GET | `/api/employee/search/{tenantId}/by-lastname` | Search by last name |
| GET | `/api/employee/search/{tenantId}/by-company` | Search by company |
| GET | `/api/employee/search/{tenantId}/by-position` | Search by position |
| GET | `/api/employee/search/{tenantId}/by-department` | Search by department |
| POST | `/api/employee/upsert` | Create employee |
| PUT | `/api/employee/upsert/{id}` | Update employee + invalidate cache |
| DELETE | `/api/employee/upsert/{id}` | Delete employee + invalidate cache |
| GET | `/api/health` | Health check |
| GET | `/api/image/search/{tenantId}` | Get images |
| GET | `/api/userentity/search/{email}` | Get user-entity mappings |

## Tech Stack

- .NET 8 / ASP.NET Core
- AWS CloudFront (CDN + caching)
- AWS Lambda@Edge (request authentication)
- AWS Secrets Manager (HMAC key storage)
- HMAC-SHA256 signed cookies for tenant context

## Prerequisites

- .NET 8 SDK
- AWS credentials configured (for Secrets Manager and CloudFront access)

## Running Locally

```bash
cd CF-Cache-API/CF-Cache-API
dotnet run
```

The API starts on `http://localhost:5100` by default.

## Deployment

Use the included scripts for EC2 deployment:

```bash
# Publish and deploy to EC2
publish-ec2.bat
deploy-ec2.sh
```

## Project Structure

```
CF-Cache-API/
├── CF-Cache-API/          # ASP.NET Core API
│   ├── Controllers/       # API controllers
│   ├── Models/            # Data models (Employee, User, Entity, etc.)
│   ├── Services/          # Business logic & AWS integrations
│   └── Program.cs         # App entry point
├── lambda-edge-auth/      # Lambda@Edge auth function
│   └── Function.cs        # Cookie validation & header injection
└── .github/workflows/     # CI/CD
```
