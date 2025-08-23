#!/bin/bash

# Stock Trading Application - Stop Script for macOS
echo "🛑 Stopping Stock Trading Application..."

# Function to stop service by PID file
stop_service() {
    local service_name=$1
    local pid_file="logs/${service_name}.pid"
    
    if [ -f "$pid_file" ]; then
        local pid=$(cat "$pid_file")
        if kill -0 "$pid" 2>/dev/null; then
            echo "Stopping $service_name (PID: $pid)..."
            kill "$pid"
            rm "$pid_file"
        else
            echo "$service_name was not running"
            rm "$pid_file" 2>/dev/null
        fi
    else
        echo "No PID file found for $service_name"
    fi
}

# Stop all services
stop_service "frontend"
stop_service "apigateway"
stop_service "portfolio"
stop_service "tradingengine"
stop_service "marketdata"
stop_service "usermanagement"

# Kill any remaining processes on our ports
echo "🧹 Cleaning up any remaining processes..."
lsof -ti:3000 | xargs kill -9 2>/dev/null || true
lsof -ti:5000 | xargs kill -9 2>/dev/null || true
lsof -ti:5001 | xargs kill -9 2>/dev/null || true
lsof -ti:5002 | xargs kill -9 2>/dev/null || true
lsof -ti:5003 | xargs kill -9 2>/dev/null || true
lsof -ti:5004 | xargs kill -9 2>/dev/null || true

echo "✅ All services stopped successfully!"
