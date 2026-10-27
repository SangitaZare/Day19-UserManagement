using AutoMapper;
using UserManagement.Day19.Dtos;
using UserManagement.Day19.Models;

namespace UserManagement.Day19.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity -> DTO (custom rule: CreatedAt becomes a formatted string called CreatedOn)
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.CreatedOn,
                       opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm")));

        // DTO -> Entity (Id and CreatedAt are set by us, not the client)
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }
}