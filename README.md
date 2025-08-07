# Block Ticket Backend - Microservices Architecture

Hệ thống quản lý vé blockchain sử dụng kiến trúc microservices với .NET 9, được thiết kế để xử lý việc bán vé, xác thực và giao dịch blockchain một cách hiệu quả và có thể mở rộng.

## 🏗️ Kiến trúc Tổng quan

### Microservices
1. **Identity Service** (Port 5001) - Quản lý người dùng và xác thực với OpenIddict
2. **Event Service** (Port 5002) - Quản lý sự kiện và thông tin vé
3. **Ticketing Service** (Port 5003) - Xử lý logic mua vé và thanh toán
4. **Blockchain Orchestrator** (Worker Service) - Giao tiếp với blockchain
5. **Resale Service** (Port 5004) - Quản lý việc trả vé và danh sách chờ
6. **Verification Service** (Port 5005) - Xác minh vé tại cổng vào
7. **Notification Service** (Worker Service) - Gửi thông báo
8. **API Gateway** (Port 5000) - Điều hướng và xác thực với YARP

### Infrastructure
- **PostgreSQL** - Database chính cho từng service
- **Redis** - Caching và queue management
- **RabbitMQ** - Message bus cho giao tiếp bất đồng bộ
- **Ganache** - Local blockchain development
- **Prometheus & Grafana** - Monitoring và visualization

## 🚀 Cách Setup và Chạy

### Yêu cầu
- .NET 9 SDK
- Docker & Docker Compose
- Visual Studio 2022 hoặc VS Code

### 1. Clone và Setup
```bash
git clone <repository-url>
cd Block_Ticket_BE
```

### 2. Khởi động Infrastructure
```bash
cd docker
docker-compose -f docker-compose.infrastructure.yml up -d
```

Điều này sẽ khởi động:
- PostgreSQL instances (ports 5432, 5433, 5434)
- Redis (port 6379)
- RabbitMQ (port 5672, management UI: 15672)
- Ganache blockchain (port 8545)
- Prometheus (port 9090)
- Grafana (port 3000)

### 3. Build Solution
```bash
dotnet restore
dotnet build
```

### 4. Run Migrations
```bash
# Identity Service
cd src/Services/Identity
dotnet ef database update

# Event Service
cd ../Event
dotnet ef migrations add InitialCreate
dotnet ef database update

# Ticketing Service
cd ../Ticketing
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> **✅ Status Update**: All three main services (Identity, Event, Ticketing) have been successfully migrated to .NET 9 and databases are created with the following schemas:
> - **BlockTicket_Identity**: ASP.NET Identity with ApplicationUser (DONE ✅)
> - **BlockTicket_Event**: Events and TicketTypes tables (DONE ✅)  
> - **BlockTicket_Ticketing**: Tickets and TicketTransactions tables (DONE ✅)

### 5. Chạy Services (Development)

#### Cách 1: Sử dụng multiple startup trong Visual Studio
- Set multiple startup projects trong solution properties
- Chọn tất cả API projects và worker services

#### Cách 2: Chạy từng service trong terminal riêng biệt
```bash
# Terminal 1 - API Gateway
cd src/ApiGateway
dotnet run

# Terminal 2 - Identity Service
cd src/Services/Identity
dotnet run

# Terminal 3 - Event Service
cd src/Services/Event
dotnet run

# Terminal 4 - Ticketing Service
cd src/Services/Ticketing
dotnet run

# Terminal 5 - Blockchain Orchestrator
cd src/Services/BlockchainOrchestrator
dotnet run

# Terminal 6 - Other services...
```

## 📊 Monitoring

### Prometheus
- URL: http://localhost:9090
- Metrics từ tất cả services sẽ được thu thập tự động

### Grafana
- URL: http://localhost:3000
- Login: admin/admin
- Import dashboard cho .NET applications

### RabbitMQ Management
- URL: http://localhost:15672
- Login: guest/guest

## 🔧 Configuration

### Connection Strings
Tất cả services được cấu hình để kết nối với infrastructure containers:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=BlockTicket_ServiceName;Username=postgres;Password=postgres",
    "RabbitMQ": "amqp://guest:guest@localhost:5672/",
    "Redis": "localhost:6379"
  }
}
```

### Blockchain Configuration
```json
{
  "Blockchain": {
    "RpcUrl": "http://localhost:8545",
    "ContractAddress": "0x1234567890123456789012345678901234567890",
    "PrivateKey": "0xprivate_key_here"
  }
}
```

## 🔄 Message Flow

### Ticket Purchase Flow
1. **User** → API Gateway → **Ticketing Service** (Purchase request)
2. **Ticketing Service** → RabbitMQ (MintTicketCommand)
3. **Blockchain Orchestrator** ← RabbitMQ (Consumes command)
4. **Blockchain Orchestrator** → Blockchain (Mint NFT)
5. **Blockchain Orchestrator** → RabbitMQ (TicketMinted event)
6. **Notification Service** ← RabbitMQ (Send confirmation)

### Events & Commands
- **Commands**: MintTicketCommand, BurnTicketCommand
- **Events**: UserRegistered, TicketPurchased, TicketMinted, YourTurnInWaitingList

## 🧪 Testing

### API Testing với Swagger
- API Gateway: http://localhost:5000/swagger
- Identity Service: http://localhost:5001/swagger
- Event Service: http://localhost:5002/swagger
- Ticketing Service: http://localhost:5003/swagger

### Sample API Calls

#### 1. Register User
```bash
POST http://localhost:5000/api/identity/auth/register
{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "John",
  "lastName": "Doe",
  "userType": 0,
  "walletAddress": "0x1234..."
}
```

#### 2. Create Event
```bash
POST http://localhost:5000/api/events
{
  "name": "Concert ABC",
  "description": "Amazing concert",
  "venue": "Stadium XYZ",
  "eventDate": "2024-12-31T20:00:00Z",
  "saleStartDate": "2024-10-01T00:00:00Z",
  "saleEndDate": "2024-12-30T23:59:59Z",
  "totalTickets": 1000,
  "ticketPrice": 100.00,
  "imageUrl": "https://example.com/image.jpg",
  "promoterId": "guid-here"
}
```

#### 3. Purchase Ticket
```bash
POST http://localhost:5000/api/tickets/purchase
{
  "eventId": "event-guid",
  "userId": "user-guid",
  "price": 100.00,
  "paymentMethod": "CreditCard",
  "userWalletAddress": "0x1234..."
}
```

## 📁 Project Structure

```
Block_Ticket_BE/
├── src/
│   ├── Services/
│   │   ├── Identity/           # User management & authentication
│   │   ├── Event/              # Event management
│   │   ├── Ticketing/          # Ticket purchasing logic
│   │   ├── BlockchainOrchestrator/  # Blockchain operations
│   │   ├── Resale/             # Ticket resale & waiting list
│   │   ├── Verification/       # Ticket verification
│   │   └── Notification/       # Notification service
│   ├── ApiGateway/             # YARP reverse proxy
│   └── Shared/
│       ├── Common/             # Shared utilities
│       └── Contracts/          # Events & Commands
├── docker/                     # Infrastructure containers
├── k8s/                        # Kubernetes manifests
└── BlockTicket.sln            # Solution file
```

## 🔒 Security

- JWT Bearer token authentication
- OpenIddict OAuth 2.0 / OIDC server
- Rate limiting via API Gateway
- Input validation và sanitization
- Blockchain transaction signing

## 📈 Scaling

- Mỗi service có thể scale độc lập
- Redis cluster để cache distribution
- PostgreSQL read replicas
- Message queue clustering với RabbitMQ
- Kubernetes deployment ready

## 🤝 Contributing

1. Fork the project
2. Create feature branch
3. Commit changes
4. Push to branch
5. Create Pull Request

## 📝 License

This project is licensed under the MIT License.
