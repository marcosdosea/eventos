using AutoMapper;
using Core;
using Core.DTO;

namespace EventoWeb.Mappers
{
    public class ParticipacaoPessoaEventoProfile : Profile
    {
        public ParticipacaoPessoaEventoProfile()
        {
            CreateMap<Participacaopessoaevento, ParticipacaoPessoaEventoDTO>().ReverseMap();
        }
    }
}
