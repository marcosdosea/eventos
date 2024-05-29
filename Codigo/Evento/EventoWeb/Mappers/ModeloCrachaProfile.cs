using AutoMapper;
using Core;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    public class ModeloCrachaProfile : Profile
    {
        public ModeloCrachaProfile()
        {
            CreateMap<ModelocrachaModel,Modelocracha>().ReverseMap();
        }
    }
}
