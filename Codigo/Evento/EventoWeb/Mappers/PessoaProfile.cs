using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers;

public class PessoaProfile : Profile
{
    public PessoaProfile()
    {
        CreateMap<PessoaModel, Pessoa>().ReverseMap();
    }
}