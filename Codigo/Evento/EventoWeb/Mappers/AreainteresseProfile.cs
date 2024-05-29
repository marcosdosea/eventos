using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers;

public class AreainteresseProfile : Profile
{
    public AreainteresseProfile()
    {
        CreateMap<AreaInteresseModel, Areainteresse>().ReverseMap();
    }
}