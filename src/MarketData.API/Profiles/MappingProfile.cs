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

        CreateMap<MarketNews, MarketNewsDto>()
            .ForMember(dest => dest.RelatedSymbols, opt => opt.MapFrom(src => 
                string.IsNullOrEmpty(src.RelatedSymbols) 
                    ? new List<string>() 
                    : src.RelatedSymbols.Split(',').ToList()));

        CreateMap<MarketIndex, MarketIndexDto>();

        CreateMap<EconomicIndicator, EconomicIndicatorDto>();
    }
}
