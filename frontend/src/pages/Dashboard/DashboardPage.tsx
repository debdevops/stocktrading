import React from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Paper,
  List,
  ListItem,
  ListItemText,
  Chip,
  IconButton,
  Button,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  AccountBalanceWallet,
  ShowChart,
  Refresh,
  Add,
} from '@mui/icons-material';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { useQuery } from 'react-query';
import { portfolioAPI, marketDataAPI, aiAPI } from '../../services/api';
import LoadingSpinner from '../../components/Common/LoadingSpinner';

const DashboardPage: React.FC = () => {
  const { data: portfolioSummary, isLoading: portfolioLoading } = useQuery(
    'portfolioSummary',
    portfolioAPI.getPortfolioSummary
  );

  const { data: marketOverview, isLoading: marketLoading } = useQuery(
    'marketOverview',
    marketDataAPI.getMarketOverview
  );

  const { data: marketInsights, isLoading: insightsLoading } = useQuery(
    'marketInsights',
    aiAPI.getMarketInsights
  );

  const { data: trendingPredictions, isLoading: predictionsLoading } = useQuery(
    'trendingPredictions',
    () => aiAPI.getTrendingPredictions(5)
  );

  // Mock chart data
  const chartData = [
    { name: 'Jan', value: 10000 },
    { name: 'Feb', value: 11000 },
    { name: 'Mar', value: 10500 },
    { name: 'Apr', value: 12000 },
    { name: 'May', value: 11800 },
    { name: 'Jun', value: 13000 },
  ];

  if (portfolioLoading || marketLoading) {
    return <LoadingSpinner message="Loading dashboard..." />;
  }

  const summary = portfolioSummary?.data || {
    totalValue: 0,
    totalGainLoss: 0,
    totalGainLossPercent: 0,
    dayGainLoss: 0,
    dayGainLossPercent: 0,
    cashBalance: 0,
    portfolioCount: 0,
  };

  return (
    <Box sx={{ flexGrow: 1 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          Dashboard
        </Typography>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => {/* Navigate to create portfolio */}}
        >
          New Portfolio
        </Button>
      </Box>

      <Grid container spacing={3}>
        {/* Portfolio Summary Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="body2">
                    Total Portfolio Value
                  </Typography>
                  <Typography variant="h5" component="div" fontWeight="bold">
                    ${summary.totalValue.toLocaleString()}
                  </Typography>
                </Box>
                <AccountBalanceWallet color="primary" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="body2">
                    Total Gain/Loss
                  </Typography>
                  <Typography 
                    variant="h5" 
                    component="div" 
                    fontWeight="bold"
                    color={summary.totalGainLoss >= 0 ? 'success.main' : 'error.main'}
                  >
                    ${summary.totalGainLoss.toLocaleString()}
                  </Typography>
                  <Typography 
                    variant="body2" 
                    color={summary.totalGainLoss >= 0 ? 'success.main' : 'error.main'}
                  >
                    {summary.totalGainLossPercent >= 0 ? '+' : ''}{summary.totalGainLossPercent.toFixed(2)}%
                  </Typography>
                </Box>
                {summary.totalGainLoss >= 0 ? 
                  <TrendingUp color="success" sx={{ fontSize: 40 }} /> :
                  <TrendingDown color="error" sx={{ fontSize: 40 }} />
                }
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="body2">
                    Day Gain/Loss
                  </Typography>
                  <Typography 
                    variant="h5" 
                    component="div" 
                    fontWeight="bold"
                    color={summary.dayGainLoss >= 0 ? 'success.main' : 'error.main'}
                  >
                    ${summary.dayGainLoss.toLocaleString()}
                  </Typography>
                  <Typography 
                    variant="body2" 
                    color={summary.dayGainLoss >= 0 ? 'success.main' : 'error.main'}
                  >
                    {summary.dayGainLoss >= 0 ? '+' : ''}{summary.dayGainLossPercent.toFixed(2)}%
                  </Typography>
                </Box>
                <ShowChart color="primary" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="body2">
                    Cash Balance
                  </Typography>
                  <Typography variant="h5" component="div" fontWeight="bold">
                    ${summary.cashBalance.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    {summary.portfolioCount} Portfolio{summary.portfolioCount !== 1 ? 's' : ''}
                  </Typography>
                </Box>
                <AccountBalanceWallet color="secondary" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Portfolio Performance Chart */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" fontWeight="bold">
                Portfolio Performance
              </Typography>
              <IconButton>
                <Refresh />
              </IconButton>
            </Box>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip formatter={(value) => [`$${value.toLocaleString()}`, 'Value']} />
                <Line 
                  type="monotone" 
                  dataKey="value" 
                  stroke="#1976d2" 
                  strokeWidth={2}
                  dot={{ fill: '#1976d2', strokeWidth: 2, r: 4 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>

        {/* AI Insights */}
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              AI Market Insights
            </Typography>
            {insightsLoading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                <LoadingSpinner size={30} message="" />
              </Box>
            ) : (
              <List dense>
                {marketInsights?.data?.insights?.slice(0, 3).map((insight: any, index: number) => (
                  <ListItem key={index} sx={{ px: 0 }}>
                    <ListItemText
                      primary={insight.title}
                      secondary={insight.description}
                      primaryTypographyProps={{ fontWeight: 'medium', fontSize: '0.9rem' }}
                      secondaryTypographyProps={{ fontSize: '0.8rem' }}
                    />
                  </ListItem>
                )) || (
                  <Typography variant="body2" color="textSecondary">
                    No insights available
                  </Typography>
                )}
              </List>
            )}
          </Paper>
        </Grid>

        {/* Trending Predictions */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              AI Stock Predictions
            </Typography>
            {predictionsLoading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                <LoadingSpinner size={30} message="" />
              </Box>
            ) : (
              <List dense>
                {trendingPredictions?.data?.predictions?.map((prediction: any, index: number) => (
                  <ListItem key={index} sx={{ px: 0, py: 1 }}>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="body1" fontWeight="bold">
                            {prediction.symbol}
                          </Typography>
                          <Chip
                            label={prediction.recommendation}
                            size="small"
                            color={
                              prediction.recommendation === 'buy' ? 'success' :
                              prediction.recommendation === 'sell' ? 'error' : 'default'
                            }
                          />
                        </Box>
                      }
                      secondary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                          <Typography variant="body2">
                            Current: ${prediction.current_price?.toFixed(2)}
                          </Typography>
                          <Typography 
                            variant="body2"
                            color={prediction.trend === 'bullish' ? 'success.main' : 'error.main'}
                          >
                            7d: ${prediction.predicted_price_7d?.toFixed(2)}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                )) || (
                  <Typography variant="body2" color="textSecondary">
                    No predictions available
                  </Typography>
                )}
              </List>
            )}
          </Paper>
        </Grid>

        {/* Recent Activity */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              Recent Activity
            </Typography>
            <List dense>
              <ListItem sx={{ px: 0 }}>
                <ListItemText
                  primary="Bought 100 shares of AAPL"
                  secondary="2 hours ago • $150.25"
                  primaryTypographyProps={{ fontSize: '0.9rem' }}
                  secondaryTypographyProps={{ fontSize: '0.8rem' }}
                />
              </ListItem>
              <ListItem sx={{ px: 0 }}>
                <ListItemText
                  primary="Sold 50 shares of GOOGL"
                  secondary="1 day ago • $2,750.00"
                  primaryTypographyProps={{ fontSize: '0.9rem' }}
                  secondaryTypographyProps={{ fontSize: '0.8rem' }}
                />
              </ListItem>
              <ListItem sx={{ px: 0 }}>
                <ListItemText
                  primary="Added TSLA to watchlist"
                  secondary="2 days ago"
                  primaryTypographyProps={{ fontSize: '0.9rem' }}
                  secondaryTypographyProps={{ fontSize: '0.8rem' }}
                />
              </ListItem>
            </List>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default DashboardPage;
