import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import { Add, Cancel, Refresh } from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { tradingAPI, marketDataAPI } from '../../services/api';
import LoadingSpinner from '../../components/Common/LoadingSpinner';

const orderSchema = yup.object({
  symbol: yup.string().required('Symbol is required'),
  orderType: yup.string().required('Order type is required'),
  side: yup.string().required('Side is required'),
  quantity: yup.number().positive('Quantity must be positive').required('Quantity is required'),
  price: yup.number().when('orderType', {
    is: 'limit',
    then: (schema) => schema.positive('Price must be positive').required('Price is required'),
    otherwise: (schema) => schema.notRequired(),
  }),
  timeInForce: yup.string().required('Time in force is required'),
});

type OrderFormData = yup.InferType<typeof orderSchema>;

const TradingPage: React.FC = () => {
  const [orderDialogOpen, setOrderDialogOpen] = useState(false);
  const [selectedSymbol, setSelectedSymbol] = useState('');
  const queryClient = useQueryClient();

  const { data: orders, isLoading: ordersLoading } = useQuery(
    'orders',
    tradingAPI.getOrders
  );

  const { data: positions, isLoading: positionsLoading } = useQuery(
    'positions',
    tradingAPI.getPositions
  );

  const { data: trades, isLoading: tradesLoading } = useQuery(
    'trades',
    tradingAPI.getTrades
  );

  const { data: quote, isLoading: quoteLoading } = useQuery(
    ['quote', selectedSymbol],
    () => marketDataAPI.getQuote(selectedSymbol),
    { enabled: !!selectedSymbol }
  );

  const createOrderMutation = useMutation(tradingAPI.createOrder, {
    onSuccess: () => {
      toast.success('Order placed successfully!');
      queryClient.invalidateQueries('orders');
      queryClient.invalidateQueries('positions');
      setOrderDialogOpen(false);
      reset();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to place order');
    },
  });

  const cancelOrderMutation = useMutation(tradingAPI.cancelOrder, {
    onSuccess: () => {
      toast.success('Order cancelled successfully!');
      queryClient.invalidateQueries('orders');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to cancel order');
    },
  });

  const {
    control,
    handleSubmit,
    watch,
    reset,
    formState: { errors },
  } = useForm<OrderFormData>({
    resolver: yupResolver(orderSchema),
    defaultValues: {
      orderType: 'market',
      side: 'buy',
      timeInForce: 'day',
    },
  });

  const watchedOrderType = watch('orderType');
  const watchedSymbol = watch('symbol');

  React.useEffect(() => {
    if (watchedSymbol) {
      setSelectedSymbol(watchedSymbol.toUpperCase());
    }
  }, [watchedSymbol]);

  const onSubmit = (data: OrderFormData) => {
    createOrderMutation.mutate({
      ...data,
      symbol: data.symbol.toUpperCase(),
    });
  };

  const handleCancelOrder = (orderId: string) => {
    cancelOrderMutation.mutate(orderId);
  };

  if (ordersLoading || positionsLoading || tradesLoading) {
    return <LoadingSpinner message="Loading trading data..." />;
  }

  const getOrderStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'filled': return 'success';
      case 'cancelled': return 'error';
      case 'pending': return 'warning';
      default: return 'default';
    }
  };

  return (
    <Box sx={{ flexGrow: 1 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          Trading
        </Typography>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setOrderDialogOpen(true)}
        >
          New Order
        </Button>
      </Box>

      <Grid container spacing={3}>
        {/* Current Positions */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" fontWeight="bold">
                Current Positions
              </Typography>
              <IconButton>
                <Refresh />
              </IconButton>
            </Box>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Symbol</TableCell>
                    <TableCell align="right">Quantity</TableCell>
                    <TableCell align="right">Avg Cost</TableCell>
                    <TableCell align="right">Current Price</TableCell>
                    <TableCell align="right">Market Value</TableCell>
                    <TableCell align="right">Unrealized P&L</TableCell>
                    <TableCell align="right">% Change</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {positions?.data?.length > 0 ? positions.data.map((position: any) => (
                    <TableRow key={position.symbol}>
                      <TableCell component="th" scope="row">
                        <Typography fontWeight="bold">{position.symbol}</Typography>
                      </TableCell>
                      <TableCell align="right">{position.quantity}</TableCell>
                      <TableCell align="right">${position.averageCost?.toFixed(2)}</TableCell>
                      <TableCell align="right">${position.currentPrice?.toFixed(2)}</TableCell>
                      <TableCell align="right">${position.marketValue?.toFixed(2)}</TableCell>
                      <TableCell 
                        align="right"
                        sx={{ 
                          color: position.unrealizedPnL >= 0 ? 'success.main' : 'error.main',
                          fontWeight: 'bold'
                        }}
                      >
                        ${position.unrealizedPnL?.toFixed(2)}
                      </TableCell>
                      <TableCell 
                        align="right"
                        sx={{ 
                          color: position.unrealizedPnLPercent >= 0 ? 'success.main' : 'error.main',
                          fontWeight: 'bold'
                        }}
                      >
                        {position.unrealizedPnLPercent >= 0 ? '+' : ''}{position.unrealizedPnLPercent?.toFixed(2)}%
                      </TableCell>
                    </TableRow>
                  )) : (
                    <TableRow>
                      <TableCell colSpan={7} align="center">
                        <Typography variant="body2" color="textSecondary">
                          No positions found
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Grid>

        {/* Open Orders */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              Open Orders
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Symbol</TableCell>
                    <TableCell>Side</TableCell>
                    <TableCell>Qty</TableCell>
                    <TableCell>Price</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Action</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {orders?.data?.filter((order: any) => order.status === 'pending')?.map((order: any) => (
                    <TableRow key={order.id}>
                      <TableCell>{order.symbol}</TableCell>
                      <TableCell>
                        <Chip 
                          label={order.side} 
                          size="small" 
                          color={order.side === 'buy' ? 'success' : 'error'}
                        />
                      </TableCell>
                      <TableCell>{order.quantity}</TableCell>
                      <TableCell>
                        {order.orderType === 'market' ? 'Market' : `$${order.price?.toFixed(2)}`}
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={order.status} 
                          size="small" 
                          color={getOrderStatusColor(order.status)}
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton 
                          size="small" 
                          onClick={() => handleCancelOrder(order.id)}
                          disabled={cancelOrderMutation.isLoading}
                        >
                          <Cancel />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  )) || (
                    <TableRow>
                      <TableCell colSpan={6} align="center">
                        <Typography variant="body2" color="textSecondary">
                          No open orders
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Grid>

        {/* Recent Trades */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              Recent Trades
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Symbol</TableCell>
                    <TableCell>Side</TableCell>
                    <TableCell>Qty</TableCell>
                    <TableCell>Price</TableCell>
                    <TableCell>Time</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {trades?.data?.slice(0, 10)?.map((trade: any) => (
                    <TableRow key={trade.id}>
                      <TableCell>{trade.symbol}</TableCell>
                      <TableCell>
                        <Chip 
                          label={trade.side} 
                          size="small" 
                          color={trade.side === 'buy' ? 'success' : 'error'}
                        />
                      </TableCell>
                      <TableCell>{trade.quantity}</TableCell>
                      <TableCell>${trade.price?.toFixed(2)}</TableCell>
                      <TableCell>
                        {new Date(trade.executedAt).toLocaleTimeString()}
                      </TableCell>
                    </TableRow>
                  )) || (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        <Typography variant="body2" color="textSecondary">
                          No recent trades
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Grid>
      </Grid>

      {/* Order Dialog */}
      <Dialog 
        open={orderDialogOpen} 
        onClose={() => setOrderDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Place New Order</DialogTitle>
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogContent>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <Controller
                  name="symbol"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      fullWidth
                      label="Symbol"
                      error={!!errors.symbol}
                      helperText={errors.symbol?.message}
                      onChange={(e) => field.onChange(e.target.value.toUpperCase())}
                    />
                  )}
                />
              </Grid>

              {selectedSymbol && quote?.data && (
                <Grid item xs={12}>
                  <Card variant="outlined">
                    <CardContent sx={{ py: 2 }}>
                      <Typography variant="h6">{selectedSymbol}</Typography>
                      <Typography variant="h4" color="primary">
                        ${quote.data.price?.toFixed(2)}
                      </Typography>
                      <Typography 
                        variant="body2" 
                        color={quote.data.change >= 0 ? 'success.main' : 'error.main'}
                      >
                        {quote.data.change >= 0 ? '+' : ''}${quote.data.change?.toFixed(2)} 
                        ({quote.data.changePercent >= 0 ? '+' : ''}{quote.data.changePercent?.toFixed(2)}%)
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              <Grid item xs={6}>
                <Controller
                  name="side"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Side</InputLabel>
                      <Select {...field} label="Side">
                        <MenuItem value="buy">Buy</MenuItem>
                        <MenuItem value="sell">Sell</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid item xs={6}>
                <Controller
                  name="orderType"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Order Type</InputLabel>
                      <Select {...field} label="Order Type">
                        <MenuItem value="market">Market</MenuItem>
                        <MenuItem value="limit">Limit</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid item xs={6}>
                <Controller
                  name="quantity"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      fullWidth
                      label="Quantity"
                      type="number"
                      error={!!errors.quantity}
                      helperText={errors.quantity?.message}
                    />
                  )}
                />
              </Grid>

              {watchedOrderType === 'limit' && (
                <Grid item xs={6}>
                  <Controller
                    name="price"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        fullWidth
                        label="Price"
                        type="number"
                        step="0.01"
                        error={!!errors.price}
                        helperText={errors.price?.message}
                      />
                    )}
                  />
                </Grid>
              )}

              <Grid item xs={12}>
                <Controller
                  name="timeInForce"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Time in Force</InputLabel>
                      <Select {...field} label="Time in Force">
                        <MenuItem value="day">Day</MenuItem>
                        <MenuItem value="gtc">Good Till Cancelled</MenuItem>
                        <MenuItem value="ioc">Immediate or Cancel</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOrderDialogOpen(false)}>Cancel</Button>
            <Button 
              type="submit" 
              variant="contained"
              disabled={createOrderMutation.isLoading}
            >
              {createOrderMutation.isLoading ? 'Placing...' : 'Place Order'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
};

export default TradingPage;
