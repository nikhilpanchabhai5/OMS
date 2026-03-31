# Order Management System (OMS) — Microservices

A microservices-based Order Management System built with .NET 10, demonstrating real-world architecture patterns.

## Architecture
```
Client
  ↓
API Gateway (YARP)
  ↓              ↓
ProductService   OrderService
  ↓                ↓
ProductDB        OrderDB
       ↓        ↓
    Azure Service Bus
```

## Services

| Service | Description | Port |
|---|---|---|
| ApiGateway | Single entry point via YARP reverse proxy | 7172 |
| ProductService | Manages product catalog and stock | 7190 |
| OrderService | Places orders and publishes events | 7220 |

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core (Code First)
- SQL Server (Docker)
- Azure Service Bus
- YARP Reverse Proxy
- Scalar (OpenAPI UI)
- Docker

## Communication Patterns

- **Sync** — OrderService calls ProductService via HTTP REST to validate stock before placing an order
- **Async** — OrderService publishes `OrderPlaced` event to Azure Service Bus. ProductService subscribes and reduces stock automatically

## Design Patterns Used

- Database per Service
- API Gateway
- Publisher / Subscriber
- Background Service (for queue consumer)

## Local Setup

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- Azure Service Bus namespace with a queue named `order-placed`

### 1. Run SQL Server in Docker
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Admin@1234" -p 1433:1433 --name sqlserver -v sqlserver-data:/var/opt/mssql -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Configure User Secrets

**ProductService**
```bash
cd ProductService
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-sql-connection"
dotnet user-secrets set "ServiceBus:ConnectionString" "your-servicebus-connection"
```

**OrderService**
```bash
cd OrderService
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-sql-connection"
dotnet user-secrets set "ServiceBus:ConnectionString" "your-servicebus-connection"
dotnet user-secrets set "ProductServiceUrl" "https://localhost:7190"
```

### 3. Run the project

Open `OMS.slnx` in Visual Studio → Set Multiple Startup Projects → Start all three services.

## API Endpoints (via Gateway)

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/products | Get all products |
| POST | /api/products | Add a product |
| PATCH | /api/products/{id}/reduce-stock | Reduce stock |
| GET | /api/orders | Get all orders |
| POST | /api/orders | Place an order |

## Coming Soon

- JWT Authentication (AuthService)
- CI/CD Pipeline (Azure DevOps)
- Docker Compose
- Azure Function (Order notification)