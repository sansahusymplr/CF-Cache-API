# CF-Cache-API

A .NET 8 multi-tenant REST API with CloudFront caching and Lambda@Edge authentication.

## Architecture

- **CF-Cache-API** — ASP.NET Core Web API (origin server behind CloudFront, dual-origin deployment)
- **lambda-edge-auth** — Lambda@Edge function (C#) that validates `TenantCtx` cookies and injects headers
- **lambda-edge-auth-python** — Lambda@Edge function (Python) with IP validation, tenant URI rewriting

### Auth Flow

1. User logs in via `POST /api/auth/upsert/login` → API mints an HMAC-signed `TenantCtx` cookie with encrypted client IP
2. Subsequent requests pass through CloudFront → Lambda@Edge (viewer-request) validates cookie signature, expiry, and client IP
3. On success, Lambda@Edge injects `X-Tenant-Id` and `X-Entity` headers before forwarding to origin
4. On mutation (update/delete), the API triggers CloudFront cache invalidation
5. If cookie is used from a different IP → **401 IP Mismatch** (stolen cookie protection)

### IP Security

- Client IP is encrypted using HMAC-XOR (no external libraries needed) and stored in the `TenantCtx` cookie
- Encryption: `HMAC-SHA256(secret, random_nonce)` → XOR with IP → `base64url(nonce + cipher)`
- Lambda@Edge decrypts the IP and compares against `event.Records[0].cf.request.clientIp` (unforgeable, set by CloudFront at TCP level)
- Cookie IP cannot be seen or tampered with by the user

### Request Flow (No Cookie)

- Requests without a `TenantCtx` cookie pass through Lambda@Edge without header injection
- The origin API rejects them with 400 (`X-Tenant-Id header is required`) — double protection

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
- AWS CloudFront (CDN + caching, dual-origin)
- AWS Lambda@Edge (viewer-request authentication + IP validation)
- AWS Secrets Manager (HMAC key storage)
- HMAC-SHA256 signed cookies for tenant context
- HMAC-XOR encrypted client IP (zero external dependencies)

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

Deploy to both EC2 origins using the automated script:

```bash
cd CF-Cache-API/CF-Cache-API
deploy-auto.bat
```

This publishes, SCPs to both origins, and restarts the services:
- **Origin 1**: us-east-2
- **Origin 2**: us-west-2

## Project Structure

```
CF-Cache-API/
├── CF-Cache-API/              # ASP.NET Core API
│   ├── Controllers/           # API controllers
│   ├── Models/                # Data models (Employee, User, Entity, TenantCtx, etc.)
│   ├── Services/              # Business logic & AWS integrations
│   │   ├── CloudFrontService  # Cache invalidation
│   │   ├── TenantCtxService   # Cookie minting + IP encryption
│   │   ├── SecretsService     # AWS Secrets Manager integration
│   │   └── ...
│   ├── deploy-auto.bat        # Automated dual-origin deployment
│   └── Program.cs             # App entry point
├── lambda-edge-auth/          # Lambda@Edge auth function (C#)
│   └── Function.cs            # Cookie validation + header injection
├── lambda-edge-auth-python/   # Lambda@Edge auth function (Python)
│   └── lambda_function.py     # Cookie + IP validation, URI rewriting
└── .github/workflows/         # CI/CD
```
