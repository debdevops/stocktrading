import numpy as np
import pandas as pd
import yfinance as yf
from datetime import datetime, timedelta
from typing import List, Dict, Any, Optional
import logging
from scipy.optimize import minimize
from sklearn.covariance import LedoitWolf
import warnings
warnings.filterwarnings('ignore')

from models.portfolio_models import (
    PortfolioOptimizationResponse, AllocationRecommendation, RiskMetrics,
    RiskTolerance, OptimizationObjective, PortfolioConstraint
)

logger = logging.getLogger(__name__)

class PortfolioOptimizer:
    def __init__(self):
        self.risk_free_rate = 0.02  # 2% risk-free rate
        self.trading_days = 252
        
    async def optimize(self, symbols: List[str], risk_tolerance: RiskTolerance,
                      investment_amount: float, constraints: List[PortfolioConstraint] = None) -> PortfolioOptimizationResponse:
        """Optimize portfolio allocation using Modern Portfolio Theory"""
        try:
            # Fetch historical data
            data = await self._fetch_portfolio_data(symbols)
            
            # Calculate returns and covariance matrix
            returns = data.pct_change().dropna()
            mean_returns = returns.mean() * self.trading_days
            cov_matrix = self._calculate_covariance_matrix(returns)
            
            # Set optimization objective based on risk tolerance
            objective = self._get_optimization_objective(risk_tolerance)
            
            # Optimize portfolio
            optimal_weights = await self._optimize_weights(
                mean_returns, cov_matrix, objective, constraints or []
            )
            
            # Calculate portfolio metrics
            portfolio_return = np.sum(optimal_weights * mean_returns)
            portfolio_volatility = np.sqrt(np.dot(optimal_weights.T, np.dot(cov_matrix * self.trading_days, optimal_weights)))
            sharpe_ratio = (portfolio_return - self.risk_free_rate) / portfolio_volatility
            
            # Get current prices for share calculations
            current_prices = await self._get_current_prices(symbols)
            
            # Create allocation recommendations
            allocations = []
            for i, symbol in enumerate(symbols):
                weight = optimal_weights[i]
                allocation_amount = investment_amount * weight
                current_price = current_prices.get(symbol, 0)
                shares = int(allocation_amount / current_price) if current_price > 0 else 0
                
                # Get company info
                company_info = await self._get_company_info(symbol)
                
                allocations.append(AllocationRecommendation(
                    symbol=symbol,
                    company_name=company_info.get('name', symbol),
                    recommended_weight=float(weight),
                    recommended_shares=shares,
                    recommended_amount=float(allocation_amount),
                    expected_return=float(mean_returns[i]),
                    risk_contribution=float(self._calculate_risk_contribution(optimal_weights, cov_matrix, i)),
                    sector=company_info.get('sector', 'Unknown')
                ))
            
            # Calculate additional risk metrics
            risk_metrics = RiskMetrics(
                portfolio_volatility=float(portfolio_volatility),
                value_at_risk_95=float(self._calculate_var(returns, optimal_weights)),
                expected_return=float(portfolio_return),
                sharpe_ratio=float(sharpe_ratio),
                max_drawdown=float(self._calculate_max_drawdown(returns, optimal_weights)),
                beta=float(self._calculate_portfolio_beta(returns, optimal_weights))
            )
            
            # Generate rebalancing suggestions
            rebalancing_suggestions = self._generate_rebalancing_suggestions(
                optimal_weights, risk_tolerance
            )
            
            # Calculate diversification score
            diversification_score = self._calculate_diversification_score(optimal_weights)
            
            return PortfolioOptimizationResponse(
                allocations=allocations,
                risk_metrics=risk_metrics,
                total_investment=investment_amount,
                expected_annual_return=float(portfolio_return),
                optimization_score=float(sharpe_ratio),
                rebalancing_suggestions=rebalancing_suggestions,
                diversification_score=diversification_score,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"Portfolio optimization failed: {str(e)}")
            raise
    
    async def _fetch_portfolio_data(self, symbols: List[str], period: str = "2y") -> pd.DataFrame:
        """Fetch historical data for portfolio symbols"""
        try:
            data = yf.download(symbols, period=period, progress=False)['Adj Close']
            
            if isinstance(data, pd.Series):
                data = data.to_frame(symbols[0])
            
            # Handle missing data
            data = data.fillna(method='forward').fillna(method='backward')
            
            return data
            
        except Exception as e:
            logger.error(f"Failed to fetch portfolio data: {str(e)}")
            raise
    
    def _calculate_covariance_matrix(self, returns: pd.DataFrame) -> np.ndarray:
        """Calculate covariance matrix using Ledoit-Wolf shrinkage"""
        try:
            # Use Ledoit-Wolf shrinkage for better covariance estimation
            lw = LedoitWolf()
            cov_matrix = lw.fit(returns).covariance_
            return cov_matrix
        except Exception:
            # Fallback to sample covariance
            return returns.cov().values
    
    def _get_optimization_objective(self, risk_tolerance: RiskTolerance) -> OptimizationObjective:
        """Get optimization objective based on risk tolerance"""
        if risk_tolerance == RiskTolerance.CONSERVATIVE:
            return OptimizationObjective.MIN_RISK
        elif risk_tolerance == RiskTolerance.AGGRESSIVE:
            return OptimizationObjective.MAX_RETURN
        else:
            return OptimizationObjective.MAX_SHARPE
    
    async def _optimize_weights(self, mean_returns: pd.Series, cov_matrix: np.ndarray,
                               objective: OptimizationObjective, constraints: List[PortfolioConstraint]) -> np.ndarray:
        """Optimize portfolio weights"""
        n_assets = len(mean_returns)
        
        # Initial guess (equal weights)
        x0 = np.array([1/n_assets] * n_assets)
        
        # Constraints
        constraints_list = [
            {'type': 'eq', 'fun': lambda x: np.sum(x) - 1}  # Weights sum to 1
        ]
        
        # Add custom constraints
        for constraint in constraints:
            if constraint.type == 'max_weight' and constraint.symbol:
                try:
                    idx = mean_returns.index.get_loc(constraint.symbol)
                    constraints_list.append({
                        'type': 'ineq',
                        'fun': lambda x, i=idx, val=constraint.value: val - x[i]
                    })
                except KeyError:
                    logger.warning(f"Symbol {constraint.symbol} not found in portfolio")
            elif constraint.type == 'min_weight' and constraint.symbol:
                try:
                    idx = mean_returns.index.get_loc(constraint.symbol)
                    constraints_list.append({
                        'type': 'ineq',
                        'fun': lambda x, i=idx, val=constraint.value: x[i] - val
                    })
                except KeyError:
                    logger.warning(f"Symbol {constraint.symbol} not found in portfolio")
        
        # Bounds (0 <= weight <= 1)
        bounds = tuple((0, 1) for _ in range(n_assets))
        
        # Objective function
        if objective == OptimizationObjective.MAX_RETURN:
            objective_func = lambda x: -np.sum(x * mean_returns)
        elif objective == OptimizationObjective.MIN_RISK:
            objective_func = lambda x: np.sqrt(np.dot(x.T, np.dot(cov_matrix * self.trading_days, x)))
        elif objective == OptimizationObjective.MAX_SHARPE:
            objective_func = lambda x: -(np.sum(x * mean_returns) - self.risk_free_rate) / np.sqrt(np.dot(x.T, np.dot(cov_matrix * self.trading_days, x)))
        else:  # EQUAL_WEIGHT
            return x0
        
        # Optimize
        try:
            result = minimize(
                objective_func,
                x0,
                method='SLSQP',
                bounds=bounds,
                constraints=constraints_list,
                options={'maxiter': 1000}
            )
            
            if result.success:
                return result.x
            else:
                logger.warning("Optimization failed, using equal weights")
                return x0
                
        except Exception as e:
            logger.error(f"Optimization error: {str(e)}")
            return x0
    
    async def _get_current_prices(self, symbols: List[str]) -> Dict[str, float]:
        """Get current prices for symbols"""
        try:
            prices = {}
            for symbol in symbols:
                ticker = yf.Ticker(symbol)
                hist = ticker.history(period="1d")
                if not hist.empty:
                    prices[symbol] = hist['Close'].iloc[-1]
                else:
                    prices[symbol] = 0.0
            return prices
        except Exception as e:
            logger.error(f"Failed to get current prices: {str(e)}")
            return {symbol: 0.0 for symbol in symbols}
    
    async def _get_company_info(self, symbol: str) -> Dict[str, str]:
        """Get company information"""
        try:
            ticker = yf.Ticker(symbol)
            info = ticker.info
            return {
                'name': info.get('longName', symbol),
                'sector': info.get('sector', 'Unknown')
            }
        except Exception:
            return {'name': symbol, 'sector': 'Unknown'}
    
    def _calculate_risk_contribution(self, weights: np.ndarray, cov_matrix: np.ndarray, asset_idx: int) -> float:
        """Calculate risk contribution of an asset"""
        portfolio_variance = np.dot(weights.T, np.dot(cov_matrix, weights))
        marginal_contrib = np.dot(cov_matrix, weights)[asset_idx]
        risk_contrib = weights[asset_idx] * marginal_contrib / portfolio_variance
        return risk_contrib
    
    def _calculate_var(self, returns: pd.DataFrame, weights: np.ndarray, confidence_level: float = 0.05) -> float:
        """Calculate Value at Risk"""
        try:
            portfolio_returns = (returns * weights).sum(axis=1)
            var = np.percentile(portfolio_returns, confidence_level * 100)
            return abs(var)
        except Exception:
            return 0.05  # Default 5% VaR
    
    def _calculate_max_drawdown(self, returns: pd.DataFrame, weights: np.ndarray) -> float:
        """Calculate maximum drawdown"""
        try:
            portfolio_returns = (returns * weights).sum(axis=1)
            cumulative = (1 + portfolio_returns).cumprod()
            running_max = cumulative.expanding().max()
            drawdown = (cumulative - running_max) / running_max
            return abs(drawdown.min())
        except Exception:
            return 0.1  # Default 10% max drawdown
    
    def _calculate_portfolio_beta(self, returns: pd.DataFrame, weights: np.ndarray) -> float:
        """Calculate portfolio beta against market (using SPY as proxy)"""
        try:
            # Fetch SPY data as market proxy
            spy = yf.download('SPY', period='2y', progress=False)['Adj Close']
            spy_returns = spy.pct_change().dropna()
            
            # Align dates
            common_dates = returns.index.intersection(spy_returns.index)
            if len(common_dates) < 50:  # Need sufficient data
                return 1.0
            
            portfolio_returns = (returns.loc[common_dates] * weights).sum(axis=1)
            market_returns = spy_returns.loc[common_dates]
            
            # Calculate beta
            covariance = np.cov(portfolio_returns, market_returns)[0, 1]
            market_variance = np.var(market_returns)
            
            beta = covariance / market_variance if market_variance > 0 else 1.0
            return beta
            
        except Exception:
            return 1.0  # Default beta
    
    def _generate_rebalancing_suggestions(self, weights: np.ndarray, risk_tolerance: RiskTolerance) -> List[str]:
        """Generate rebalancing suggestions"""
        suggestions = []
        
        # Check for concentration risk
        max_weight = np.max(weights)
        if max_weight > 0.4:
            suggestions.append("Consider reducing concentration in top holding to improve diversification")
        
        # Check for minimum viable positions
        min_weight = np.min(weights[weights > 0])
        if min_weight < 0.02:
            suggestions.append("Consider eliminating positions below 2% to reduce transaction costs")
        
        # Risk tolerance specific suggestions
        if risk_tolerance == RiskTolerance.CONSERVATIVE:
            suggestions.append("Consider monthly rebalancing to maintain risk targets")
        elif risk_tolerance == RiskTolerance.AGGRESSIVE:
            suggestions.append("Quarterly rebalancing may be sufficient for growth-focused portfolio")
        else:
            suggestions.append("Rebalance quarterly or when allocations drift more than 5% from targets")
        
        return suggestions
    
    def _calculate_diversification_score(self, weights: np.ndarray) -> float:
        """Calculate diversification score (0-1, higher is better)"""
        # Use Herfindahl-Hirschman Index inverse
        hhi = np.sum(weights ** 2)
        max_hhi = 1.0  # Completely concentrated
        min_hhi = 1.0 / len(weights)  # Equally weighted
        
        # Normalize to 0-1 scale
        diversification_score = (max_hhi - hhi) / (max_hhi - min_hhi)
        return max(0.0, min(1.0, diversification_score))
