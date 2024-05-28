using AutoMapper;
using Core;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    public class SubeventoProfile : Profile
    {           
        public SubeventoProfile()            
        {                
            CreateMap<SubeventoModel, Subevento>().ReverseMap();    
        }
    }

}

