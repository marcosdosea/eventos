using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers;

public class InscricaoProfile : Profile
{
    public InscricaoProfile()
    {
        CreateMap<InscricaoEventoModel, Inscricaopessoaevento>().ReverseMap();
    }
}