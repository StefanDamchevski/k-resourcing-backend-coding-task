using AutoMapper;
using Claims.Application.Dto;
using Claims.Domain.Entities;

namespace Claims.Application.Mappings
{
    public class ClaimProfile : Profile
    {
        public ClaimProfile()
        {
            CreateMap<Claim, ClaimDto>()
                .ReverseMap();

            CreateMap<Cover, CoverDto>()
                .ReverseMap();
        }
    }
}