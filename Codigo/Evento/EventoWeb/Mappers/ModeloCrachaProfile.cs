using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers
{
    public class ModeloCrachaProfile : Profile
    {
        public ModeloCrachaProfile()
        {
            CreateMap<ModelocrachaModel, Modelocracha>()
                .ForMember(dest => dest.Logotipo, opt => opt.MapFrom(src => src.Logotipo))
                .ReverseMap();
        }
    }


}