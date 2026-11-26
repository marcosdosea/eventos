using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class ModelocertificadoModel
    {
        public uint Id { get; set; }

        [Range(typeof(uint), "1", "4294967295", ErrorMessage = "Selecione o evento")]
        [Display(Name = "Evento")]
        public uint IdEvento { get; set; }

        [Required, StringLength(500)]
        [Display(Name = "Texto antes do participante")]
        public string TextoAntesParticipante { get; set; } = "Certificamos que";

        [Required, StringLength(500)]
        [Display(Name = "Texto antes do evento")]
        public string TextoAntesEvento { get; set; } = "participou do evento";

        // Para exibição
        [Display(Name = "Evento")]
        public string NomeEvento { get; set; } = string.Empty;

        // Listas para dropdown
        public SelectList Eventos { get; set; } = new SelectList(Array.Empty<object>(), "Id", "Nome");
    }
}
