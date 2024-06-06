using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers
{
    public class GestorEventoProfile : Profile
    {
        public GestorEventoProfile()
        {
            CreateMap<GestorEventoModel, Pessoa>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.NomeCracha, opt => opt.MapFrom(src => src.NomeCracha))
                .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.Cpf))
                .ForMember(dest => dest.Telefone1, opt => opt.MapFrom(src => src.Telefone1))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
        }
    }
}