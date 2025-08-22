import axios, { AxiosInstance, AxiosResponse } from 'axios';

// API Base URL - points to API Gateway
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

// Create axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid
      localStorage.removeItem('authToken');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Auth API
export const authAPI = {
  login: (credentials: { email: string; password: string }) =>
    apiClient.post('/api/auth/login', credentials),
  
  register: (userData: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    confirmPassword: string;
  }) => apiClient.post('/api/auth/register', userData),
  
  refreshToken: (token: string) =>
    apiClient.post('/api/auth/refresh', { token }),
  
  verifyEmail: (token: string) =>
    apiClient.post('/api/auth/verify-email', { token }),
};

// User API
export const userAPI = {
  getProfile: () => apiClient.get('/api/users/profile'),
  
  updateProfile: (profileData: {
    firstName: string;
    lastName: string;
    phoneNumber?: string;
    dateOfBirth?: string;
  }) => apiClient.put('/api/users/profile', profileData),
  
  changePassword: (passwordData: {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  }) => apiClient.put('/api/users/change-password', passwordData),
};

// Market Data API
export const marketDataAPI = {
  getQuote: (symbol: string) =>
    apiClient.get(`/api/marketdata/quote/${symbol}`),
  
  getQuotes: (symbols: string[]) =>
    apiClient.post('/api/marketdata/quotes', { symbols }),
  
  searchStocks: (query: string) =>
    apiClient.get(`/api/marketdata/search?query=${query}`),
  
  getHistoricalData: (symbol: string, period: string) =>
    apiClient.get(`/api/marketdata/historical/${symbol}?period=${period}`),
  
  getChartData: (symbol: string, interval: string, range: string) =>
    apiClient.get(`/api/marketdata/chart/${symbol}?interval=${interval}&range=${range}`),
  
  getMarketOverview: () =>
    apiClient.get('/api/marketdata/overview'),
  
  getMarketNews: (limit?: number) =>
    apiClient.get(`/api/marketdata/news${limit ? `?limit=${limit}` : ''}`),
  
  getTechnicalIndicators: (symbol: string, indicators: string[]) =>
    apiClient.post(`/api/marketdata/technical/${symbol}`, { indicators }),
  
  scanMarket: (criteria: any) =>
    apiClient.post('/api/marketdata/scan', criteria),
};

// Trading API
export const tradingAPI = {
  createOrder: (orderData: {
    symbol: string;
    orderType: string;
    side: string;
    quantity: number;
    price?: number;
    timeInForce?: string;
  }) => apiClient.post('/api/orders', orderData),
  
  getOrders: (filters?: any) =>
    apiClient.get('/api/orders', { params: filters }),
  
  getOrder: (orderId: string) =>
    apiClient.get(`/api/orders/${orderId}`),
  
  cancelOrder: (orderId: string) =>
    apiClient.delete(`/api/orders/${orderId}`),
  
  getTrades: (filters?: any) =>
    apiClient.get('/api/trades', { params: filters }),
  
  getPositions: () =>
    apiClient.get('/api/positions'),
  
  getPosition: (symbol: string) =>
    apiClient.get(`/api/positions/${symbol}`),
};

// Watchlist API
export const watchlistAPI = {
  getWatchlists: () =>
    apiClient.get('/api/watchlists'),
  
  createWatchlist: (watchlistData: { name: string; description?: string }) =>
    apiClient.post('/api/watchlists', watchlistData),
  
  getWatchlist: (watchlistId: string) =>
    apiClient.get(`/api/watchlists/${watchlistId}`),
  
  updateWatchlist: (watchlistId: string, watchlistData: { name: string; description?: string }) =>
    apiClient.put(`/api/watchlists/${watchlistId}`, watchlistData),
  
  deleteWatchlist: (watchlistId: string) =>
    apiClient.delete(`/api/watchlists/${watchlistId}`),
  
  addToWatchlist: (watchlistId: string, symbol: string) =>
    apiClient.post(`/api/watchlists/${watchlistId}/items`, { symbol }),
  
  removeFromWatchlist: (watchlistId: string, itemId: string) =>
    apiClient.delete(`/api/watchlists/${watchlistId}/items/${itemId}`),
};

// Portfolio API
export const portfolioAPI = {
  getPortfolios: () =>
    apiClient.get('/api/portfolios'),
  
  createPortfolio: (portfolioData: {
    name: string;
    description?: string;
    initialCash: number;
  }) => apiClient.post('/api/portfolios', portfolioData),
  
  getPortfolio: (portfolioId: string) =>
    apiClient.get(`/api/portfolios/${portfolioId}`),
  
  updatePortfolio: (portfolioId: string, portfolioData: {
    name: string;
    description?: string;
  }) => apiClient.put(`/api/portfolios/${portfolioId}`, portfolioData),
  
  deletePortfolio: (portfolioId: string) =>
    apiClient.delete(`/api/portfolios/${portfolioId}`),
  
  getPortfolioSummary: () =>
    apiClient.get('/api/portfolios/summary'),
  
  getPortfolioAnalytics: (portfolioId: string) =>
    apiClient.get(`/api/portfolios/${portfolioId}/analytics`),
  
  getRebalanceRecommendation: (portfolioId: string) =>
    apiClient.post(`/api/portfolios/${portfolioId}/rebalance`),
  
  updatePortfolioValues: (portfolioId: string) =>
    apiClient.post(`/api/portfolios/${portfolioId}/update-values`),
};

// Transaction API
export const transactionAPI = {
  getTransactions: (filters?: any) =>
    apiClient.get('/api/transactions', { params: filters }),
  
  createTransaction: (transactionData: {
    portfolioId: string;
    symbol: string;
    transactionType: string;
    quantity: number;
    price: number;
  }) => apiClient.post('/api/transactions', transactionData),
  
  getTransaction: (transactionId: string) =>
    apiClient.get(`/api/transactions/${transactionId}`),
  
  deleteTransaction: (transactionId: string) =>
    apiClient.delete(`/api/transactions/${transactionId}`),
};

// AI Services API
export const aiAPI = {
  predictPrice: (predictionData: {
    symbol: string;
    daysAhead: number;
    modelType: string;
  }) => apiClient.post('/api/ai/predictions/price', predictionData),
  
  analyzeTechnical: (analysisData: {
    symbol: string;
    period: string;
    indicators: string[];
  }) => apiClient.post('/api/ai/analysis/technical', analysisData),
  
  analyzeSentiment: (sentimentData: {
    symbols: string[];
    sources: string[];
    timeRange: string;
  }) => apiClient.post('/api/ai/sentiment/analyze', sentimentData),
  
  optimizePortfolio: (optimizationData: {
    symbols: string[];
    riskTolerance: string;
    investmentAmount: number;
    constraints?: any[];
  }) => apiClient.post('/api/ai/portfolio/optimize', optimizationData),
  
  getTrendingPredictions: (limit?: number) =>
    apiClient.get(`/api/ai/predictions/trending${limit ? `?limit=${limit}` : ''}`),
  
  getMarketInsights: () =>
    apiClient.get('/api/ai/market/insights'),
  
  getStockRecommendations: (symbol: string) =>
    apiClient.get(`/api/ai/recommendations/${symbol}`),
};

export default apiClient;
