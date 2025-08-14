using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class InscricaopessoaeventoModel
    {
        [Display(Name = "Evento")]
        public uint IdEvento { get; set; }

        [Display(Name = "Nome do Evento")]
        public string NomeEvento { get; set; } = null!;

        [Display(Name = "Banner do Evento")]
        public string BannerUrl { get; set; } = null!;

        [Display(Name = "Data de Início")]
        public DateTime DataEvento { get; set; }

        [Display(Name = "Data de Término")]
        public DateTime DataFimEvento { get; set; }

        [Display(Name = "Local do Evento")]
        public string LocalEvento { get; set; } = null!;

        [Display(Name = "Descrição do Evento")]
        public string DescricaoEvento { get; set; } = null!;

        [Display(Name = "Lotes")]
        public List<LoteInscricaoModel> Lotes { get; set; } = new();

        [Display(Name = "Total Selecionado")]
        public decimal TotalSelecionado { get; set; }

        [Display(Name = "Cupom Promocional")]
        public string? CupomPromocional { get; set; }
    }

    public class LoteInscricaoModel
    {
        [Display(Name = "Código do Lote")]
        public string Id { get; set; } = null!;

        [Display(Name = "Nome do Lote")]
        public string NomeLote { get; set; } = null!;

        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = null!;

        [Display(Name = "Preço")]
        public decimal Preco { get; set; }

        [Display(Name = "Quantidade")]
        public int Quantidade { get; set; }
    }
}
