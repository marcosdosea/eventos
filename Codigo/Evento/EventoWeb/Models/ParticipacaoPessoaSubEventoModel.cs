// EventoWeb/Models/ParticipacaoPessoaSubEventoModel.cs
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class ParticipacaoPessoaSubEventoModel
    {
        public uint Id { get; set; }

        [Required(ErrorMessage = "Selecione a Pessoa")]
        [Display(Name = "Pessoa")]
        public uint IdPessoa { get; set; }

        [Display(Name = "Sub-Evento")]
        public uint IdSubEvento { get; set; }

        [Required(ErrorMessage = "Informe a Entrada")]
        [Display(Name = "Hora de Entrada")]
        [DataType(DataType.DateTime)]
        public DateTime Entrada { get; set; }

        [Display(Name = "Hora de Saída")]
        [DataType(DataType.DateTime)]
        public DateTime? Saida { get; set; }

        [Display(Name = "Pessoa")]
        public string NomePessoa { get; set; } = string.Empty;

        [Display(Name = "Sub-Evento")]
        public string NomeSubEvento { get; set; } = string.Empty;

        public SelectList Pessoas { get; set; } = new SelectList(Array.Empty<object>(), "Id", "NomePessoa");
    }
}
