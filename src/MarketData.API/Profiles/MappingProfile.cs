using AutoMapper;
using MarketData.API.DTOs;
using MarketData.API.Models;

namespace MarketData.API.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Stock, StockDto>()
            .ForMember(dest => dest.CurrentQuote, opt => opt.Ignore());

        CreateMap<Quote, QuoteDto>();

        CreateMap<HistoricalPrice, HistoricalPriceDto>();

        CreateMap<MarketNews, MarketNewsDto>();

        CreateMap<MarketIndex, MarketIndexDto>();

        CreateMap<EconomicIndicator, EconomicIndicatorDto>();
    }
}
