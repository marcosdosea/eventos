using AutoMapper;
using Core;

namespace EventoWeb.Models
{
    public class TipoInscricaoProfile : Profile
    {
        public TipoInscricaoProfile() {
            CreateMap<TipoInscricaoModel, Tipoinscricao>().ReverseMap();
        }
    }
}
