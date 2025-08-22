using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TradingEngine.API.Data;
using TradingEngine.API.DTOs;
using TradingEngine.API.Models;

namespace TradingEngine.API.Services;

public interface IWatchlistService
{
    Task<WatchlistDto> CreateWatchlistAsync(Guid userId, CreateWatchlistDto watchlistDto);
    Task<List<WatchlistDto>> GetWatchlistsAsync(Guid userId);
    Task<WatchlistDto> GetWatchlistAsync(Guid userId, Guid watchlistId);
    Task<WatchlistDto> AddItemToWatchlistAsync(Guid userId, Guid watchlistId, AddWatchlistItemDto itemDto);
    Task<bool> RemoveItemFromWatchlistAsync(Guid userId, Guid watchlistId, string symbol);
    Task<bool> DeleteWatchlistAsync(Guid userId, Guid watchlistId);
    Task<WatchlistDto> UpdateWatchlistAsync(Guid userId, Guid watchlistId, CreateWatchlistDto watchlistDto);
}

public class WatchlistService : IWatchlistService
{
    private readonly TradingDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMarketDataService _marketDataService;

    public WatchlistService(TradingDbContext context, IMapper mapper, IMarketDataService marketDataService)
    {
        _context = context;
        _mapper = mapper;
        _marketDataService = marketDataService;
    }

    public async Task<WatchlistDto> CreateWatchlistAsync(Guid userId, CreateWatchlistDto watchlistDto)
    {
        // Check if user already has a watchlist with this name
        if (await _context.Watchlists.AnyAsync(w => w.UserId == userId && w.Name == watchlistDto.Name))
        {
            throw new ArgumentException("A watchlist with this name already exists");
        }

        var watchlist = new Watchlist
        {
            UserId = userId,
            Name = watchlistDto.Name,
            Description = watchlistDto.Description,
            IsDefault = watchlistDto.IsDefault
        };

        // If this is set as default, unset other default watchlists
        if (watchlistDto.IsDefault)
        {
            var existingDefaults = await _context.Watchlists
                .Where(w => w.UserId == userId && w.IsDefault)
                .ToListAsync();
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        _context.Watchlists.Add(watchlist);
        await _context.SaveChangesAsync();

        return await GetWatchlistAsync(userId, watchlist.Id);
    }

    public async Task<List<WatchlistDto>> GetWatchlistsAsync(Guid userId)
    {
        var watchlists = await _context.Watchlists
            .Include(w => w.Items)
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Name)
            .ToListAsync();

        var watchlistDtos = new List<WatchlistDto>();

        foreach (var watchlist in watchlists)
        {
            var watchlistDto = _mapper.Map<WatchlistDto>(watchlist);
            
            // Enrich with market data
            foreach (var item in watchlistDto.Items)
            {
                var marketData = await _marketDataService.GetQuoteAsync(item.Symbol);
                if (marketData != null)
                {
                    item.CurrentPrice = marketData.Price;
                    item.DayChange = marketData.Change;
                    item.DayChangePercent = marketData.ChangePercent;
                    item.Volume = marketData.Volume;
                }
            }

            watchlistDtos.Add(watchlistDto);
        }

        return watchlistDtos;
    }

    public async Task<WatchlistDto> GetWatchlistAsync(Guid userId, Guid watchlistId)
    {
        var watchlist = await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
        {
            throw new KeyNotFoundException("Watchlist not found");
        }

        var watchlistDto = _mapper.Map<WatchlistDto>(watchlist);

        // Enrich with market data
        foreach (var item in watchlistDto.Items)
        {
            var marketData = await _marketDataService.GetQuoteAsync(item.Symbol);
            if (marketData != null)
            {
                item.CurrentPrice = marketData.Price;
                item.DayChange = marketData.Change;
                item.DayChangePercent = marketData.ChangePercent;
                item.Volume = marketData.Volume;
            }
        }

        return watchlistDto;
    }

    public async Task<WatchlistDto> AddItemToWatchlistAsync(Guid userId, Guid watchlistId, AddWatchlistItemDto itemDto)
    {
        var watchlist = await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
        {
            throw new KeyNotFoundException("Watchlist not found");
        }

        // Check if symbol already exists in watchlist
        if (watchlist.Items.Any(i => i.Symbol == itemDto.Symbol.ToUpper()))
        {
            throw new ArgumentException("Symbol already exists in this watchlist");
        }

        // Validate symbol
        var marketData = await _marketDataService.GetQuoteAsync(itemDto.Symbol);
        if (marketData == null)
        {
            throw new ArgumentException($"Invalid symbol: {itemDto.Symbol}");
        }

        var maxSortOrder = watchlist.Items.Any() ? watchlist.Items.Max(i => i.SortOrder) : 0;

        var watchlistItem = new WatchlistItem
        {
            WatchlistId = watchlistId,
            Symbol = itemDto.Symbol.ToUpper(),
            SortOrder = maxSortOrder + 1,
            AlertPrice = itemDto.AlertPrice,
            AlertType = itemDto.AlertType,
            AlertEnabled = itemDto.AlertEnabled
        };

        _context.WatchlistItems.Add(watchlistItem);
        await _context.SaveChangesAsync();

        return await GetWatchlistAsync(userId, watchlistId);
    }

    public async Task<bool> RemoveItemFromWatchlistAsync(Guid userId, Guid watchlistId, string symbol)
    {
        var watchlist = await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
        {
            throw new KeyNotFoundException("Watchlist not found");
        }

        var item = watchlist.Items.FirstOrDefault(i => i.Symbol == symbol.ToUpper());
        if (item == null)
        {
            throw new KeyNotFoundException("Symbol not found in watchlist");
        }

        _context.WatchlistItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteWatchlistAsync(Guid userId, Guid watchlistId)
    {
        var watchlist = await _context.Watchlists
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
        {
            throw new KeyNotFoundException("Watchlist not found");
        }

        _context.Watchlists.Remove(watchlist);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<WatchlistDto> UpdateWatchlistAsync(Guid userId, Guid watchlistId, CreateWatchlistDto watchlistDto)
    {
        var watchlist = await _context.Watchlists
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
        {
            throw new KeyNotFoundException("Watchlist not found");
        }

        // Check if another watchlist with this name exists
        if (await _context.Watchlists.AnyAsync(w => w.UserId == userId && w.Name == watchlistDto.Name && w.Id != watchlistId))
        {
            throw new ArgumentException("A watchlist with this name already exists");
        }

        watchlist.Name = watchlistDto.Name;
        watchlist.Description = watchlistDto.Description;
        watchlist.UpdatedAt = DateTime.UtcNow;

        // Handle default flag
        if (watchlistDto.IsDefault && !watchlist.IsDefault)
        {
            var existingDefaults = await _context.Watchlists
                .Where(w => w.UserId == userId && w.IsDefault && w.Id != watchlistId)
                .ToListAsync();
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        watchlist.IsDefault = watchlistDto.IsDefault;

        await _context.SaveChangesAsync();

        return await GetWatchlistAsync(userId, watchlistId);
    }
}
