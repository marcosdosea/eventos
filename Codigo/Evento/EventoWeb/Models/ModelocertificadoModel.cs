using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class ModelocertificadoModel
    {
        public uint Id { get; set; }

        [Required(ErrorMessage = "Selecione o Evento")]
        [Display(Name = "Evento")]
        public uint IdEvento { get; set; }

        [Required(ErrorMessage = "Informe a Data de Emissão")]
        [Display(Name = "Data de Emissão")]
        [DataType(DataType.Date)]
        public DateTime DataEmissao { get; set; } = DateTime.Now;

        [Display(Name = "Código do Certificado")]
        public string Codigo { get; set; } = string.Empty;

        // Para exibição
        [Display(Name = "Evento")]
        public string NomeEvento { get; set; } = string.Empty;

        // Listas para dropdown
        public SelectList Eventos { get; set; } = new SelectList(Array.Empty<object>(), "Id", "Nome");
    }
}
