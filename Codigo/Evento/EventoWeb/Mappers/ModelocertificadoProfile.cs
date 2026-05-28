using AutoMapper;
using Core;
using Core.DTO;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    /// <summary>
    /// CORREÇÃO: este arquivo herdava de Controller (erro grave).
    /// Agora herda corretamente de Profile (AutoMapper).
    /// </summary>
    public class ModelocertificadoProfile : Profile
    {
        public ModelocertificadoProfile()
        {
            CreateMap<Modelocertificado, ModelocertificadoModel>()
                .ForMember(dest => dest.NomeEvento, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<Modelocertificado, ModelocertificadoDTO>()
                .ForMember(dest => dest.NomePessoa, opt => opt.Ignore())
                .ForMember(dest => dest.NomeEvento, opt => opt.Ignore())
                .ReverseMap();
        }
    }
}
