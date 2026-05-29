using AutoMapper;
using Core;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    public class ModelocertificadoProfile : Profile
    {
        public ModelocertificadoProfile()
        {
            CreateMap<ModelocertificadoModel, Modelocertificado>()
                .ForMember(dest => dest.LogotipoSuperior, opt => opt.Ignore())
                .ForMember(dest => dest.Assinatura1, opt => opt.Ignore())
                .ForMember(dest => dest.Assinatura2, opt => opt.Ignore())
                .ForMember(dest => dest.IdEvento, opt => opt.MapFrom(src => (uint)src.IdEvento));

            CreateMap<Modelocertificado, ModelocertificadoModel>()
                .ForMember(dest => dest.Eventos, opt => opt.Ignore())
                .ForMember(dest => dest.LogotipoSuperior, opt => opt.Ignore())
                .ForMember(dest => dest.Assinatura1, opt => opt.Ignore())
                .ForMember(dest => dest.Assinatura2, opt => opt.Ignore());
        }
    }
}
