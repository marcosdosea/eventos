// EventoWeb/Mappers/InscricaopessoaeventoProfile.cs
using AutoMapper;
using Core;
using Core.DTO;

namespace EventoWeb.Mappers
{
    public class InscricaopessoaeventoProfile : Profile
    {
        public InscricaopessoaeventoProfile()
        {
            // Mapeia entidade para DTO
            CreateMap<Inscricaopessoaevento, InscricaopessoaeventoDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IdPessoa, opt => opt.MapFrom(src => src.IdPessoa))
                .ForMember(dest => dest.IdEvento, opt => opt.MapFrom(src => src.IdEvento))
                .ForMember(dest => dest.IdPapel, opt => opt.MapFrom(src => src.IdPapel))
                .ForMember(dest => dest.IdTipoInscricao, opt => opt.MapFrom(src => src.IdTipoInscricao))
                .ForMember(dest => dest.DataInscricao, opt => opt.MapFrom(src => src.DataInscricao))
                .ForMember(dest => dest.ValorTotal, opt => opt.MapFrom(src => src.ValorTotal))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.FrequenciaFinal, opt => opt.MapFrom(src => src.FrequenciaFinal))
                .ForMember(dest => dest.NomeCracha, opt => opt.MapFrom(src => src.NomeCracha));
            // Se quiser incluir campos de navegação (ex: nome do evento), pode adicionar aqui:
            //.ForMember(dest => dest.NomeEvento, opt => opt.MapFrom(src => src.IdEventoNavigation.Nome));

            // Mapeia DTO para entidade
            CreateMap<InscricaopessoaeventoDTO, Inscricaopessoaevento>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IdPessoa, opt => opt.MapFrom(src => src.IdPessoa))
                .ForMember(dest => dest.IdEvento, opt => opt.MapFrom(src => src.IdEvento))
                .ForMember(dest => dest.IdPapel, opt => opt.MapFrom(src => src.IdPapel))
                .ForMember(dest => dest.IdTipoInscricao, opt => opt.MapFrom(src => src.IdTipoInscricao))
                .ForMember(dest => dest.DataInscricao, opt => opt.MapFrom(src => src.DataInscricao))
                .ForMember(dest => dest.ValorTotal, opt => opt.MapFrom(src => src.ValorTotal))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.FrequenciaFinal, opt => opt.MapFrom(src => src.FrequenciaFinal))
                .ForMember(dest => dest.NomeCracha, opt => opt.MapFrom(src => src.NomeCracha));
        }
    }
}
