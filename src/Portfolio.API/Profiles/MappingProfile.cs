using AutoMapper;
using Portfolio.API.DTOs;
using Portfolio.API.Models;

namespace Portfolio.API.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Portfolio.API.Models.Portfolio, PortfolioDto>()
            .ForMember(dest => dest.Holdings, opt => opt.Ignore())
            .ForMember(dest => dest.Allocations, opt => opt.Ignore());

        CreateMap<CreatePortfolioDto, Portfolio.API.Models.Portfolio>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentValue, opt => opt.Ignore())
            .ForMember(dest => dest.TotalGainLoss, opt => opt.Ignore())
            .ForMember(dest => dest.TotalGainLossPercent, opt => opt.Ignore())
            .ForMember(dest => dest.DayGainLoss, opt => opt.Ignore())
            .ForMember(dest => dest.DayGainLossPercent, opt => opt.Ignore())
            .ForMember(dest => dest.CashBalance, opt => opt.Ignore())
            .ForMember(dest => dest.InvestedAmount, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdated, opt => opt.Ignore());

        CreateMap<Holding, HoldingDto>()
            .ForMember(dest => dest.CompanyName, opt => opt.Ignore())
            .ForMember(dest => dest.AllocationPercent, opt => opt.Ignore());

        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.CompanyName, opt => opt.Ignore());

        CreateMap<CreateTransactionDto, Transaction>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.TotalAmount, opt => opt.Ignore());

        CreateMap<PortfolioPerformance, PortfolioPerformanceDto>();

        CreateMap<PortfolioAllocation, PortfolioAllocationDto>();

        CreateMap<Alert, AlertDto>()
            .ForMember(dest => dest.CompanyName, opt => opt.Ignore());

        CreateMap<CreateAlertDto, Alert>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.IsTriggered, opt => opt.Ignore())
            .ForMember(dest => dest.TriggeredAt, opt => opt.Ignore());

        CreateMap<DividendRecord, DividendRecordDto>()
            .ForMember(dest => dest.CompanyName, opt => opt.Ignore());
    }
}
