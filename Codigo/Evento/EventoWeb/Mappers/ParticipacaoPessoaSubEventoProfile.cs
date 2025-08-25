// EventoWeb/Mappers/ParticipacaoPessoaSubEventoProfile.cs
using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers
{
    public class ParticipacaoPessoaSubEventoProfile : Profile
    {
        public ParticipacaoPessoaSubEventoProfile()
        {
            // Mapeia entidade para ViewModel
            CreateMap<Participacaopessoasubevento, ParticipacaoPessoaSubEventoModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IdPessoa, opt => opt.MapFrom(src => src.IdPessoa))
                .ForMember(dest => dest.IdSubEvento, opt => opt.MapFrom(src => src.IdSubEvento))
                .ForMember(dest => dest.Entrada, opt => opt.MapFrom(src => src.Entrada))
                .ForMember(dest => dest.Saida, opt => opt.MapFrom(src => src.Saida))
                .ForMember(dest => dest.NomePessoa, opt => opt.MapFrom(src => src.IdPessoaNavigation.Nome))
                .ForMember(dest => dest.NomeSubEvento, opt => opt.MapFrom(src => src.IdSubEventoNavigation.Nome));


            // Mapeia ViewModel para entidade
            CreateMap<ParticipacaoPessoaSubEventoModel, Participacaopessoasubevento>()
                .ForMember(dest => dest.IdPessoa, opt => opt.MapFrom(src => src.IdPessoa))
                .ForMember(dest => dest.IdSubEvento, opt => opt.MapFrom(src => src.IdSubEvento))
                .ForMember(dest => dest.Entrada, opt => opt.MapFrom(src => src.Entrada))
                .ForMember(dest => dest.Saida, opt => opt.MapFrom(src => src.Saida));
        }
    }
}
