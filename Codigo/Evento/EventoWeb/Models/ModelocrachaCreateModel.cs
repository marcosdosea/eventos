using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class ModelocrachaCreateModel
    {
        public ModelocrachaModel Modelocracha { get; set; }
        public EventoSimpleDTO Evento { get; set; }
    }
}
