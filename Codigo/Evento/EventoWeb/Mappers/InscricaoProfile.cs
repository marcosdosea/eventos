using AutoMapper;
using Core;
using Core.DTO;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    /// <summary>
    /// Profile unificado de inscrições.
    /// Absorve InscricaopessoaeventoProfile (arquivo deletado por redundância).
    /// </summary>
    public class InscricaoProfile : Profile
    {
        public InscricaoProfile()
        {
            // Model de tela → Entidade
            CreateMap<InscricaoEventoModel, Inscricaopessoaevento>().ReverseMap();

            // Entidade → DTO (uso em APIs/controllers administrativos)
            CreateMap<Inscricaopessoaevento, InscricaopessoaeventoDTO>()
                .ForMember(dest => dest.Id,              opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IdPessoa,        opt => opt.MapFrom(src => src.IdPessoa))
                .ForMember(dest => dest.IdEvento,        opt => opt.MapFrom(src => src.IdEvento))
                .ForMember(dest => dest.IdPapel,         opt => opt.MapFrom(src => src.IdPapel))
                .ForMember(dest => dest.IdTipoInscricao, opt => opt.MapFrom(src => src.IdTipoInscricao))
                .ForMember(dest => dest.DataInscricao,   opt => opt.MapFrom(src => src.DataInscricao))
                .ForMember(dest => dest.ValorTotal,      opt => opt.MapFrom(src => src.ValorTotal))
                .ForMember(dest => dest.Status,          opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.FrequenciaFinal, opt => opt.MapFrom(src => src.FrequenciaFinal))
                .ForMember(dest => dest.NomeCracha,      opt => opt.MapFrom(src => src.NomeCracha));

            CreateMap<InscricaopessoaeventoDTO, Inscricaopessoaevento>()
                .ForMember(dest => dest.IdPessoaNavigation,       opt => opt.Ignore())
                .ForMember(dest => dest.IdEventoNavigation,       opt => opt.Ignore())
                .ForMember(dest => dest.IdPapelNavigation,        opt => opt.Ignore())
                .ForMember(dest => dest.IdTipoInscricaoNavigation,opt => opt.Ignore())
                .ForMember(dest => dest.Pagamentos,               opt => opt.Ignore());
        }
    }
}
