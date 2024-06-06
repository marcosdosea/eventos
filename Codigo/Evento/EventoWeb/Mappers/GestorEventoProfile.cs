using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers;

public class GestorEventoProfile : Profile
{
    public GestorEventoProfile()
    {
        CreateMap<GestorEventoModel, Pessoa>().ReverseMap();
    }
}