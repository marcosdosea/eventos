using AutoMapper;
using Core;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    public class ModelocertificadoProfile : Profile
    {
        public ModelocertificadoProfile()
        {
            CreateMap<Modelocertificado, ModelocertificadoModel>()
                .ForMember(destino => destino.NomeEvento, opcao => opcao.MapFrom(origem => origem.IdEventoNavigation.Nome))
                .ForMember(destino => destino.Eventos, opcao => opcao.Ignore());

            CreateMap<ModelocertificadoModel, Modelocertificado>(MemberList.None)
                .ForMember(destino => destino.Id, opcao => opcao.Ignore());
        }
    }
}
