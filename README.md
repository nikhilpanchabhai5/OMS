# Order Management System (OMS) — Microservices

A production-ready microservices-based Order Management System built with .NET 10, demonstrating real-world architecture patterns used in enterprise applications.

## Architecture

```
Client
  ↓
API Gateway (YARP) — JWT Authentication
  ↓              ↓              ↓
ProductService  OrderService  AuthService
  ↓                ↓
ProductDB        OrderDB        AuthDB
       ↓        ↓
    Azure Service Bus
```

## Services

| Service | Description | Port |
|---|---|---|
| ApiGateway | Single entry point, JWT validation via YARP | 5000 |
| ProductService | Manages product catalog and stock levels | 7190 |
| OrderService | Places orders, publishes events | 7220 |
| AuthService | User registration, login, JWT token issuing | 7100 |

## Tech Stack

| Category | Technology |
|---|---|
| Framework | .NET 10 / ASP.NET Core Web API |
| ORM | Entity Framework Core (Code First) |
| Database | SQL Server (Docker container) |
| Messaging | Azure Service Bus |
| API Gateway | YARP Reverse Proxy |
| Authentication | JWT Bearer Tokens |
| Containerization | Docker + Docker Compose |
| CI/CD | Azure DevOps Pipelines |
| Container Registry | Azure Container Registry (ACR) |
| API Documentation | Scalar (OpenAPI) |

## Communication Patterns

- **Sync** — OrderService calls ProductService via HTTP REST to validate stock availability before placing an order
- **Async** — OrderService publishes `OrderPlaced` event to Azure Service Bus. ProductService subscribes via BackgroundService and reduces stock automatically

## Design Patterns Used

- Database per Service
- API Gateway pattern
- Publisher / Subscriber
- Background Service (queue consumer)
- Retry pattern (SQL Server startup)
- JWT Authentication at Gateway level

## CI/CD Pipeline

Azure DevOps pipeline runs automatically on every push to `main`:

```
Push to main
     ↓
Stage 1: Build & Test
  - Restore packages
  - Build all services
     ↓
Stage 2: Docker Build & Push
  - Build Docker images for all 4 services
  - Push to Azure Container Registry
```

## Local Setup

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- Azure Service Bus namespace with queue named `order-placed`
- Visual Studio 2022

### 1. Clone the repository

```bash
git clone https://github.com/your-username/oms.git
cd oms
```

### 2. Create `.env` file in root folder

```env
SERVICE_BUS_CONNECTION="your-azure-service-bus-connection-string"
JWT_KEY="your-jwt-secret-key-minimum-32-characters"
```

### 3. Run with Docker Compose

```bash
docker-compose up --build
```

All services start automatically including SQL Server. Databases are created on first run.

### 4. Configure User Secrets (for Visual Studio F5)

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

**AuthService**
```bash
cd AuthService
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-sql-connection"
dotnet user-secrets set "Jwt:Key" "your-jwt-secret-key"
```

**ApiGateway**
```bash
cd ApiGateway
dotnet user-secrets set "Jwt:Key" "your-jwt-secret-key"
```

## API Endpoints

All requests go through the Gateway on port `5000`.

### Auth (no token required)

| Method | Endpoint | Description |
|---|---|---|
| POST | /api/auth/register | Register new user |
| POST | /api/auth/login | Login and get JWT token |

### Products (token required)

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/products | Get all products |
| GET | /api/products/{id} | Get product by ID |
| POST | /api/products | Add new product |
| PATCH | /api/products/{id}/reduce-stock | Reduce stock |

### Orders (token required)

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/orders | Get all orders |
| POST | /api/orders | Place new order |

## Testing with Postman

1. Register → `POST /api/auth/register`
2. Login → `POST /api/auth/login` → copy token
3. Add token to Authorization header → `Bearer your-token`
4. Add a product → `POST /api/products`
5. Place an order → `POST /api/orders`
6. Verify stock reduced → `GET /api/products/{id}`

## Security

- JWT tokens validated at Gateway level — individual services are not exposed directly
- Passwords hashed using BCrypt
- Sensitive config stored in `.env` file (excluded from source control)
- User Secrets used for local development

## Project Challenges Solved

- **Circular reference** — fixed using `[JsonIgnore]` and `ReferenceHandler.IgnoreCycles`
- **Double stock deduction** — separated sync (validate) and async (action) responsibilities
- **SQL Server startup delay** — implemented retry logic with 10 attempts and 3 second delay
- **Docker networking** — services communicate using container names as hostnames
- **Windows agent EBUSY error** — added PowerShell cleanup step in pipeline