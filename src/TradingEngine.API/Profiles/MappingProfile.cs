using AutoMapper;
using TradingEngine.API.DTOs;
using TradingEngine.API.Models;

namespace TradingEngine.API.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<CreateOrderDto, Order>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.FilledQuantity, opt => opt.Ignore())
            .ForMember(dest => dest.AveragePrice, opt => opt.Ignore());

        CreateMap<Trade, TradeDto>();
        
        CreateMap<Position, PositionDto>()
            .ForMember(dest => dest.CurrentPrice, opt => opt.Ignore())
            .ForMember(dest => dest.DayChange, opt => opt.Ignore())
            .ForMember(dest => dest.DayChangePercent, opt => opt.Ignore());

        CreateMap<Watchlist, WatchlistDto>();
        CreateMap<CreateWatchlistDto, Watchlist>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore());

        CreateMap<WatchlistItem, WatchlistItemDto>()
            .ForMember(dest => dest.CurrentPrice, opt => opt.Ignore())
            .ForMember(dest => dest.DayChange, opt => opt.Ignore())
            .ForMember(dest => dest.DayChangePercent, opt => opt.Ignore())
            .ForMember(dest => dest.Volume, opt => opt.Ignore());

        CreateMap<AddWatchlistItemDto, WatchlistItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.WatchlistId, opt => opt.Ignore())
            .ForMember(dest => dest.SortOrder, opt => opt.Ignore());
    }
}
