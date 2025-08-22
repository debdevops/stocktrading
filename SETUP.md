# Quick Setup Guide

## 🚀 Getting Started - Manual Setup

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+
- Python 3.11+
- SQL Server (LocalDB or Azure SQL)
- Visual Studio 2022 or VS Code

### Local Development Setup

#### 1. Backend Services
```bash
# Start all .NET services (run each in separate terminal)
cd src/UserManagement.API && dotnet run    # Port 5001
cd src/MarketData.API && dotnet run       # Port 5002
cd src/TradingEngine.API && dotnet run    # Port 5003
cd src/Portfolio.API && dotnet run        # Port 5004
cd src/ApiGateway && dotnet run           # Port 5000
```

#### 2. AI Services
```bash
cd src/AI.Services
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

#### 3. Frontend
```bash
cd frontend
npm install
npm start    # Port 3000
```

## 🌐 Azure App Service Deployment

### Individual Microservice Deployment

Each microservice should be deployed to its own Azure App Service:

#### 1. Create App Services
```bash
# Create resource group
az group create --name rg-stocktrading-prod --location "East US"

# Create App Service Plan
az appservice plan create --name asp-stocktrading --resource-group rg-stocktrading-prod --sku B2

# Create individual App Services
az webapp create --name stocktrading-gateway --resource-group rg-stocktrading-prod --plan asp-stocktrading --runtime "DOTNET:8.0"
az webapp create --name stocktrading-usermgmt --resource-group rg-stocktrading-prod --plan asp-stocktrading --runtime "DOTNET:8.0"
az webapp create --name stocktrading-marketdata --resource-group rg-stocktrading-prod --plan asp-stocktrading --runtime "DOTNET:8.0"
az webapp create --name stocktrading-trading --resource-group rg-stocktrading-prod --plan asp-stocktrading --runtime "DOTNET:8.0"
az webapp create --name stocktrading-portfolio --resource-group rg-stocktrading-prod --plan asp-stocktrading --runtime "DOTNET:8.0"
az webapp create --name stocktrading-ai --resource-group rg-stocktrading-prod --plan asp-stocktrading --runtime "PYTHON:3.11"
```

#### 2. Deploy Each Service
```bash
# Build and deploy each .NET service
dotnet publish src/ApiGateway -c Release -o ./publish/gateway
az webapp deployment source config-zip --resource-group rg-stocktrading-prod --name stocktrading-gateway --src ./publish/gateway.zip

dotnet publish src/UserManagement.API -c Release -o ./publish/usermgmt
az webapp deployment source config-zip --resource-group rg-stocktrading-prod --name stocktrading-usermgmt --src ./publish/usermgmt.zip

dotnet publish src/MarketData.API -c Release -o ./publish/marketdata
az webapp deployment source config-zip --resource-group rg-stocktrading-prod --name stocktrading-marketdata --src ./publish/marketdata.zip

dotnet publish src/TradingEngine.API -c Release -o ./publish/trading
az webapp deployment source config-zip --resource-group rg-stocktrading-prod --name stocktrading-trading --src ./publish/trading.zip

dotnet publish src/Portfolio.API -c Release -o ./publish/portfolio
az webapp deployment source config-zip --resource-group rg-stocktrading-prod --name stocktrading-portfolio --src ./publish/portfolio.zip

# Deploy Python AI Services
cd src/AI.Services
zip -r ../../publish/ai-services.zip . -x "venv/*" "__pycache__/*" "*.pyc"
az webapp deployment source config-zip --resource-group rg-stocktrading-prod --name stocktrading-ai --src ./publish/ai-services.zip
```

#### 3. Configure App Settings
```bash
# Configure connection strings and settings for each service
az webapp config appsettings set --resource-group rg-stocktrading-prod --name stocktrading-usermgmt --settings \
  "ConnectionStrings__DefaultConnection=Server=tcp:your-sql-server.database.windows.net,1433;Database=StockTradingUserDb;User ID=sqladmin;Password=YourPassword123!;Encrypt=True;" \
  "JwtSettings__SecretKey=your-super-secret-key-that-is-at-least-32-characters-long" \
  "JwtSettings__Issuer=StockTradingAPI" \
  "JwtSettings__Audience=StockTradingClient"

# Repeat for other services with appropriate database names
```

#### 4. Deploy Frontend to Static Web App
```bash
# Create Static Web App
az staticwebapp create --name stocktrading-frontend --resource-group rg-stocktrading-prod --source https://github.com/debdevops/stocktrading --branch main --app-location "/frontend" --output-location "build"

# Configure API URL
az staticwebapp appsettings set --name stocktrading-frontend --setting-names REACT_APP_API_URL=https://stocktrading-gateway.azurewebsites.net
```

## 🎯 Demo Credentials
- **Email**: demo@stocktrading.com
- **Password**: Demo123!

## 📊 Key Features to Test
1. **Dashboard** - Portfolio overview and AI insights
2. **Trading** - Place orders with real-time quotes
3. **Portfolio** - Create portfolios and track performance
4. **Market Data** - Live quotes and technical analysis
5. **AI Insights** - Price predictions and sentiment analysis
6. **Watchlists** - Track favorite stocks with alerts

## 🔧 Configuration

### Local Development
All services are pre-configured with default settings for local development.

### Production Configuration
- Update connection strings in Azure App Service settings
- Configure API keys as App Service environment variables
- Set up Azure SQL Database and configure connection strings
- Configure service URLs in API Gateway settings

## 🆘 Troubleshooting
- Ensure ports 3000, 5000-5004, 8000 are available for local development
- Verify .NET 8.0 SDK and Node.js 18+ are installed
- Check SQL Server LocalDB is running for local development
- For Azure deployment, ensure proper App Service configuration and connection strings
