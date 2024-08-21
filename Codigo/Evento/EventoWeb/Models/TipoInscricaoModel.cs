using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class TipoInscricaoModel
    {
        [Display(Name = "Código")]
        public uint Id { get; set; }

        [Display(Name = "Evento")]
        public uint IdEvento { get; set; }

		[Display(Name = "Nome")]
		[StringLength(50)]
		public string Nome { get; set; } = null!;

		[Display(Name = "Descrição")]
        [StringLength(200)]
        public string Descricao { get; set; } = null!;

        [Display(Name = "Valor", Prompt = "R$ 00.00")]
        [Range(0.00, double.MaxValue, ErrorMessage = "O valor da inscrição deve ser zero ou maior que zero.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Por favor, insira no máximo duas casas decimais e use '.' como separador decimal.")]
        public decimal Valor { get; set; }

        [Display(Name = "Data Inicial")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataInicio { get; set; }

        [Display(Name = "Data Final")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime Datafim { get; set; }

        [Display(Name = "Evento")]
        [Range(0, 1, ErrorMessage = "O valor deve ser Não ou Sim.")]
        public sbyte? UsadaEvento { get; set; }

        [Display(Name = "Subevento")]
        [Range(0, 1, ErrorMessage = "O valor deve ser Não ou Sim.")]
        public sbyte? UsadaSubevento { get; set; } 

        [Display(Name = "Evento")]
        public string? NomeEvento { get; set; }
        
        public SelectList Evento { get; set; }
    }
}
