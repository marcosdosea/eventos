using AutoMapper;
using Core;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    public class EventoProfile : Profile
    {
        public EventoProfile() {
            CreateMap<EventoModel, Evento>().ReverseMap();
        }
    }
}
