# Stock Trading Application - macOS Setup Guide

## ✅ Prerequisites (Already Installed)
- ✅ .NET 9.0.100 SDK
- ✅ Node.js v24.6.0  
- ✅ Python 3.13.0

## 🚀 Quick Start

### 1. Start All Services
```bash
cd /Users/debasisghosh/Desktop/myreactapps/stocktrading
./start-local.sh
```

### 2. Access the Application
- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:5000/swagger

### 3. Stop All Services
```bash
./stop-local.sh
```

## 🔐 Demo Login
- **Email**: demo@stocktrading.com
- **Password**: Demo123!

## 📊 Service Architecture

| Service | Port | Purpose |
|---------|------|---------|
| React Frontend | 3000 | User Interface |
| API Gateway | 5000 | API Router & Auth |
| UserManagement | 5001 | Authentication |
| MarketData | 5002 | Stock Quotes |
| TradingEngine | 5003 | Order Processing |
| Portfolio | 5004 | Portfolio Management |

## 🗄️ Database
- **Type**: SQLite (macOS compatible)
- **Location**: Each service creates its own .db file
- **Auto-created**: On first service startup

## 📝 Logs
All service logs are stored in `logs/` directory:
- `frontend.log`
- `apigateway.log`
- `usermanagement.log`
- `marketdata.log`
- `tradingengine.log`
- `portfolio.log`

## 🛠️ Manual Service Startup (Alternative)

If you prefer to start services individually:

```bash
# Terminal 1 - UserManagement API
cd src/UserManagement.API
dotnet run --urls=http://localhost:5001

# Terminal 2 - MarketData API  
cd src/MarketData.API
dotnet run --urls=http://localhost:5002

# Terminal 3 - TradingEngine API
cd src/TradingEngine.API
dotnet run --urls=http://localhost:5003

# Terminal 4 - Portfolio API
cd src/Portfolio.API
dotnet run --urls=http://localhost:5004

# Terminal 5 - API Gateway
cd src/ApiGateway
dotnet run --urls=http://localhost:5000

# Terminal 6 - Frontend
cd frontend
npm start
```

## 🔧 Troubleshooting

### Port Already in Use
```bash
# Kill processes on specific ports
lsof -ti:3000 | xargs kill -9
lsof -ti:5000 | xargs kill -9
# ... repeat for other ports
```

### Rebuild Solution
```bash
dotnet clean
dotnet build
```

### Reset Frontend Dependencies
```bash
cd frontend
rm -rf node_modules package-lock.json
npm install
```

## 🎯 Key Features
- ✅ User Authentication & Registration
- ✅ Real-time Stock Quotes (Mock Data)
- ✅ Portfolio Management
- ✅ Order Placement & Tracking
- ✅ Market Data Visualization
- ✅ Trading Analytics
- ✅ Responsive Material-UI Design

## 🔐 Security
- JWT Token Authentication
- CORS Enabled for Development
- SQLite Database (Local Development)

## 📱 Browser Compatibility
- Chrome (Recommended)
- Firefox
- Safari
- Edge

---

**Ready to start trading!** 🎉
