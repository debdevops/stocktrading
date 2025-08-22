using AutoMapper;
using UserManagement.API.DTOs;
using UserManagement.API.Models;

namespace UserManagement.API.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore());

        CreateMap<UserProfile, UserProfileDto>();
        
        CreateMap<UpdateUserProfileDto, UserProfile>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<RegisterUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }
}
