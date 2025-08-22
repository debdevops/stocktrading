using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using MarketData.API.DTOs;
using MarketData.API.Services;

namespace MarketData.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;

    public MarketDataController(IMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    [HttpGet("quote/{symbol}")]
    public async Task<ActionResult<ApiResponse<QuoteDto>>> GetQuote(string symbol)
    {
        var quote = await _marketDataService.GetQuoteAsync(symbol);
        
        if (quote == null)
        {
            return NotFound(ApiResponse<QuoteDto>.ErrorResponse("Quote not found for symbol"));
        }

        return Ok(ApiResponse<QuoteDto>.SuccessResponse(quote));
    }

    [HttpPost("quotes")]
    public async Task<ActionResult<ApiResponse<List<QuoteDto>>>> GetQuotes([FromBody] List<string> symbols)
    {
        var quotes = await _marketDataService.GetQuotesAsync(symbols);
        return Ok(ApiResponse<List<QuoteDto>>.SuccessResponse(quotes));
    }

    [HttpGet("stock/{symbol}")]
    public async Task<ActionResult<ApiResponse<StockDto>>> GetStock(string symbol)
    {
        var stock = await _marketDataService.GetStockAsync(symbol);
        
        if (stock == null)
        {
            return NotFound(ApiResponse<StockDto>.ErrorResponse("Stock not found"));
        }

        return Ok(ApiResponse<StockDto>.SuccessResponse(stock));
    }

    [HttpGet("stocks/search")]
    public async Task<ActionResult<ApiResponse<List<StockDto>>>> SearchStocks([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<List<StockDto>>.ErrorResponse("Search query is required"));
        }

        var stocks = await _marketDataService.SearchStocksAsync(query);
        return Ok(ApiResponse<List<StockDto>>.SuccessResponse(stocks));
    }

    [HttpGet("stocks")]
    public async Task<ActionResult<ApiResponse<List<StockDto>>>> GetStocks([FromQuery] MarketDataFilterDto filter)
    {
        var stocks = await _marketDataService.GetStocksAsync(filter);
        return Ok(ApiResponse<List<StockDto>>.SuccessResponse(stocks));
    }

    [HttpGet("historical/{symbol}")]
    public async Task<ActionResult<ApiResponse<List<HistoricalPriceDto>>>> GetHistoricalData(
        string symbol,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string interval = "1d")
    {
        var request = new HistoricalDataRequestDto
        {
            Symbol = symbol,
            StartDate = startDate,
            EndDate = endDate,
            Interval = interval
        };

        var data = await _marketDataService.GetHistoricalDataAsync(request);
        return Ok(ApiResponse<List<HistoricalPriceDto>>.SuccessResponse(data));
    }

    [HttpGet("chart/{symbol}")]
    public async Task<ActionResult<ApiResponse<ChartDataDto>>> GetChartData(
        string symbol,
        [FromQuery] string interval = "1d",
        [FromQuery] int period = 30)
    {
        var chartData = await _marketDataService.GetChartDataAsync(symbol, interval, period);
        return Ok(ApiResponse<ChartDataDto>.SuccessResponse(chartData));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<MarketOverviewDto>>> GetMarketOverview()
    {
        var overview = await _marketDataService.GetMarketOverviewAsync();
        return Ok(ApiResponse<MarketOverviewDto>.SuccessResponse(overview));
    }

    [HttpGet("indices")]
    public async Task<ActionResult<ApiResponse<List<MarketIndexDto>>>> GetMarketIndices()
    {
        var indices = await _marketDataService.GetMarketIndicesAsync();
        return Ok(ApiResponse<List<MarketIndexDto>>.SuccessResponse(indices));
    }

    [HttpGet("news")]
    public async Task<ActionResult<ApiResponse<List<MarketNewsDto>>>> GetMarketNews([FromQuery] NewsFilterDto filter)
    {
        var news = await _marketDataService.GetMarketNewsAsync(filter);
        return Ok(ApiResponse<List<MarketNewsDto>>.SuccessResponse(news));
    }

    [HttpGet("technical/{symbol}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TechnicalIndicatorsDto>>> GetTechnicalIndicators(string symbol)
    {
        var indicators = await _marketDataService.GetTechnicalIndicatorsAsync(symbol);
        
        if (indicators == null)
        {
            return NotFound(ApiResponse<TechnicalIndicatorsDto>.ErrorResponse("Insufficient data for technical analysis"));
        }

        return Ok(ApiResponse<TechnicalIndicatorsDto>.SuccessResponse(indicators));
    }

    [HttpPost("scan")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<StockDto>>>> ScanMarket([FromBody] MarketScannerDto scanner)
    {
        var results = await _marketDataService.ScanMarketAsync(scanner);
        return Ok(ApiResponse<List<StockDto>>.SuccessResponse(results));
    }
}
