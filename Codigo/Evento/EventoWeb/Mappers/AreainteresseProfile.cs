using AutoMapper;
using Core;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    public class AreainteresseProfile : Profile
    {
        private readonly IMapper _mapper;
        public AreainteresseProfile()
        {
            CreateMap<AreainteresseModel,Areainteresse>().ReverseMap();
        }
    }
}
