using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class ModelocertificadoModel
    {
        public uint Id { get; set; }

        [Required(ErrorMessage = "Logotipo superior é obrigatório")]
        [Display(Name = "Logotipo Superior")]
        public IFormFile LogotipoSuperior { get; set; } = null!;

        [Required(ErrorMessage = "Texto Antes do Participante é obrigatório")]
        [Display(Name = "Texto Antes do Participante")]
        public string TextoAntesParticipante { get; set; } = null!;

        [Required(ErrorMessage = "Texto antes do Evento é obrigatório")]
        [Display(Name = "Texto antes do Evento")]
        public string TextoAntesEvento { get; set; } = null!;

        [Required(ErrorMessage = "Texto antes da carga horária é obrigatório")]
        [Display(Name = "Texto antes da carga horária")]
        public string TextoAntesCargaHoraria { get; set; } = null!;

        [Required(ErrorMessage = "Texto da Assinatura 1 é obrigatório")]
        [Display(Name = "Texto da Assinatura 1")]
        public string TextoAssinatura1 { get; set; } = null!;

        [Required(ErrorMessage = "Assinatura 1 é obrigatória")]
        [Display(Name = "Assinatura 1")]
        public IFormFile Assinatura1 { get; set; } = null!;

        [Display(Name = "Texto da Assinatura 2")]
        public string? TextoAssinatura2 { get; set; } 

        [Display(Name = "Assinatura 2")]
        public IFormFile? Assinatura2 { get; set; }

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