using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models
{
    public class TipoInscricaoModel
    {
        [Display(Name = "Código")]
        [Required]
        [Key]
        public uint Id { get; set; }

		[Required]
		[Display(Name = "Evento")]
        public uint IdEvento { get; set; }

		[Required]
		[Display(Name = "Nome")]
		[StringLength(50)]
		public string Nome { get; set; } = null!;

		[Required]
		[Display(Name = "Descrição")]
        [StringLength(200)]
        public string Descricao { get; set; } = null!;

		[Required]
		[Display(Name = "Valor", Prompt = "R$ 00.00")]
        [Range(0.00, double.MaxValue, ErrorMessage = "O valor da inscrição deve ser zero ou maior que zero.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Por favor, insira no máximo duas casas decimais e use '.' como separador decimal.")]
        public decimal Valor { get; set; }

		[Required]
		[Display(Name = "Data Inicial")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataInicio { get; set; }

		[Required]
		[Display(Name = "Data Final")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime Datafim { get; set; }

		[Required]
		[Display(Name = "Evento")]
        [Range(1, 2, ErrorMessage = "O valor deve ser Não ou Sim.")]
        public sbyte UsadaEvento { get; set; }

		[Required]
		[Display(Name = "Subevento")]
        [Range(1, 2, ErrorMessage = "O valor deve ser Não ou Sim.")]
        public sbyte UsadaSubevento { get; set; }

		[Required]
		[Display(Name = "Evento")]
        public string NomeEvento { get; set; }
    }
}
