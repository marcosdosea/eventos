using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class ModelocrachaCreateModel
    {
        public ModelocrachaModel Modelocracha { get; set; }
        public SelectList Eventos { get; set; }
    }
}
