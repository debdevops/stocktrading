import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { io, Socket } from 'socket.io-client';
import { useAuth } from './AuthContext';

interface MarketData {
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  timestamp: string;
}

interface WebSocketContextType {
  socket: Socket | null;
  isConnected: boolean;
  marketData: { [symbol: string]: MarketData };
  subscribeToSymbol: (symbol: string) => void;
  unsubscribeFromSymbol: (symbol: string) => void;
}

const WebSocketContext = createContext<WebSocketContextType | undefined>(undefined);

export const useWebSocket = () => {
  const context = useContext(WebSocketContext);
  if (context === undefined) {
    throw new Error('useWebSocket must be used within a WebSocketProvider');
  }
  return context;
};

interface WebSocketProviderProps {
  children: ReactNode;
}

export const WebSocketProvider: React.FC<WebSocketProviderProps> = ({ children }) => {
  const [socket, setSocket] = useState<Socket | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [marketData, setMarketData] = useState<{ [symbol: string]: MarketData }>({});
  const { token, isAuthenticated } = useAuth();

  useEffect(() => {
    if (isAuthenticated && token) {
      // Initialize WebSocket connection
      const newSocket = io('ws://localhost:3001', {
        auth: {
          token: token,
        },
        transports: ['websocket'],
      });

      newSocket.on('connect', () => {
        console.log('WebSocket connected');
        setIsConnected(true);
      });

      newSocket.on('disconnect', () => {
        console.log('WebSocket disconnected');
        setIsConnected(false);
      });

      newSocket.on('marketData', (data: MarketData) => {
        setMarketData(prev => ({
          ...prev,
          [data.symbol]: data,
        }));
      });

      newSocket.on('error', (error) => {
        console.error('WebSocket error:', error);
      });

      setSocket(newSocket);

      return () => {
        newSocket.close();
        setSocket(null);
        setIsConnected(false);
      };
    }
  }, [isAuthenticated, token]);

  const subscribeToSymbol = (symbol: string) => {
    if (socket && isConnected) {
      socket.emit('subscribe', { symbol });
    }
  };

  const unsubscribeFromSymbol = (symbol: string) => {
    if (socket && isConnected) {
      socket.emit('unsubscribe', { symbol });
    }
  };

  const value = {
    socket,
    isConnected,
    marketData,
    subscribeToSymbol,
    unsubscribeFromSymbol,
  };

  return (
    <WebSocketContext.Provider value={value}>
      {children}
    </WebSocketContext.Provider>
  );
};
