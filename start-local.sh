#!/bin/bash

# Stock Trading Application - Local Startup Script for macOS
echo "🚀 Starting Stock Trading Application locally..."

# Function to check if port is available
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null ; then
        echo "❌ Port $1 is already in use"
        return 1
    else
        echo "✅ Port $1 is available"
        return 0
    fi
}

# Check required ports
echo "🔍 Checking required ports..."
check_port 3000 || exit 1  # Frontend
check_port 5000 || exit 1  # API Gateway
check_port 5001 || exit 1  # UserManagement
check_port 5002 || exit 1  # MarketData
check_port 5003 || exit 1  # TradingEngine
check_port 5004 || exit 1  # Portfolio

# Create logs directory
mkdir -p logs

echo "🏗️ Building .NET solution..."
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "🔧 Starting backend services..."

# Start UserManagement API (Port 5001)
echo "Starting UserManagement API on port 5001..."
cd src/UserManagement.API
dotnet run --urls=http://localhost:5001 > ../../logs/usermanagement.log 2>&1 &
USERMGMT_PID=$!
cd ../..

# Start MarketData API (Port 5002)
echo "Starting MarketData API on port 5002..."
cd src/MarketData.API
dotnet run --urls=http://localhost:5002 > ../../logs/marketdata.log 2>&1 &
MARKETDATA_PID=$!
cd ../..

# Start TradingEngine API (Port 5003)
echo "Starting TradingEngine API on port 5003..."
cd src/TradingEngine.API
dotnet run --urls=http://localhost:5003 > ../../logs/tradingengine.log 2>&1 &
TRADING_PID=$!
cd ../..

# Start Portfolio API (Port 5004)
echo "Starting Portfolio API on port 5004..."
cd src/Portfolio.API
dotnet run --urls=http://localhost:5004 > ../../logs/portfolio.log 2>&1 &
PORTFOLIO_PID=$!
cd ../..

# Wait for services to start
echo "⏳ Waiting for services to start..."
sleep 10

# Start API Gateway (Port 5000)
echo "Starting API Gateway on port 5000..."
cd src/ApiGateway
dotnet run --urls=http://localhost:5000 > ../../logs/apigateway.log 2>&1 &
GATEWAY_PID=$!
cd ../..

# Wait for API Gateway
sleep 5

# Start Frontend (Port 3000)
echo "Starting React Frontend on port 3000..."
cd frontend
/opt/homebrew/bin/npm start > ../logs/frontend.log 2>&1 &
FRONTEND_PID=$!
cd ..

echo ""
echo "🎉 Stock Trading Application is starting up!"
echo ""
echo "📊 Services Status:"
echo "   • API Gateway:      http://localhost:5000"
echo "   • UserManagement:   http://localhost:5001"
echo "   • MarketData:       http://localhost:5002"
echo "   • TradingEngine:    http://localhost:5003"
echo "   • Portfolio:        http://localhost:5004"
echo "   • Frontend:         http://localhost:3000"
echo ""
echo "📋 Demo Credentials:"
echo "   • Email:    demo@stocktrading.com"
echo "   • Password: Demo123!"
echo ""
echo "📝 Logs are available in the 'logs/' directory"
echo ""
echo "🛑 To stop all services, run: ./stop-local.sh"
echo ""

# Save PIDs for cleanup
echo $USERMGMT_PID > logs/usermanagement.pid
echo $MARKETDATA_PID > logs/marketdata.pid
echo $TRADING_PID > logs/tradingengine.pid
echo $PORTFOLIO_PID > logs/portfolio.pid
echo $GATEWAY_PID > logs/apigateway.pid
echo $FRONTEND_PID > logs/frontend.pid

echo "✅ All services started successfully!"
echo "🌐 Open http://localhost:3000 in your browser"
