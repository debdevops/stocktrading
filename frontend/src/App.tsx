import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { Box } from '@mui/material';

import { useAuth } from './contexts/AuthContext';
import Layout from './components/Layout/Layout';
import ProtectedRoute from './components/Auth/ProtectedRoute';

// Pages
import LoginPage from './pages/Auth/LoginPage';
import RegisterPage from './pages/Auth/RegisterPage';
import DashboardPage from './pages/Dashboard/DashboardPage';
import TradingPage from './pages/Trading/TradingPage';
import PortfolioPage from './pages/Portfolio/PortfolioPage';
import MarketDataPage from './pages/MarketData/MarketDataPage';
import AIInsightsPage from './pages/AIInsights/AIInsightsPage';
import WatchlistPage from './pages/Watchlist/WatchlistPage';
import ProfilePage from './pages/Profile/ProfilePage';
import LoadingSpinner from './components/Common/LoadingSpinner';

function App() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Routes>
        {/* Public Routes */}
        <Route 
          path="/login" 
          element={
            isAuthenticated ? <Navigate to="/dashboard" replace /> : <LoginPage />
          } 
        />
        <Route 
          path="/register" 
          element={
            isAuthenticated ? <Navigate to="/dashboard" replace /> : <RegisterPage />
          } 
        />

        {/* Protected Routes */}
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <Layout>
                <Routes>
                  <Route path="/dashboard" element={<DashboardPage />} />
                  <Route path="/trading" element={<TradingPage />} />
                  <Route path="/portfolio" element={<PortfolioPage />} />
                  <Route path="/market-data" element={<MarketDataPage />} />
                  <Route path="/ai-insights" element={<AIInsightsPage />} />
                  <Route path="/watchlist" element={<WatchlistPage />} />
                  <Route path="/profile" element={<ProfilePage />} />
                  <Route path="/" element={<Navigate to="/dashboard" replace />} />
                  <Route path="*" element={<Navigate to="/dashboard" replace />} />
                </Routes>
              </Layout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </Box>
  );
}

export default App;
