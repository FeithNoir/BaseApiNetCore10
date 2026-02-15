using AutoMapper;
using BaseApiNet10.Core.DTOs.Auth;
using BaseApiNet10.Core.Entities.Models;

namespace BaseApiNet10.Business.MappingProfiles
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<UserRegistrationRequestDto, User>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));
        }
    }
}
