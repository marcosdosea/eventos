using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class SubeventoModel
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = "Código do Subevento é obrigatório")]
        [Key]
        public uint Id { get; set; }

        [Display(Name = "Código do Evento")]
        [Required(ErrorMessage = "Código do Evento é obrigatório")]
        [Key]
        public uint IdEvento { get; set; }

        [Display(Name = "Nome")]
        [Required(ErrorMessage = "Nome do Subevento é obrigatório")]
        public string Nome { get; set; } = null!;

        [Display(Name = "Descrição")]
        [Required(ErrorMessage = "Fale um pouco a respeito desse Sub-Evento")]
        public string Descricao { get; set; } = null!;

        [Display(Name = "Data Inicial do Subvento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        [Required]
        public DateTime DataInicio { get; set; }

		[Required]
		[Display(Name = "Data Final do Subvento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataFim { get; set; }

		[Required]
		[Display(Name = "Incrição gratuita")]
        public sbyte InscricaoGratuita { get; set; }

		/// <summary>

		/// C- CADASTRO

		/// A- ABERTO

		/// F- FINALIZADO

		///  

		/// </summary>

		[Display(Name = "Status")]
        [Required(ErrorMessage = "Status do Subevento é obrigatório")]
        public string Status { get; set; } = null!;

		[Required]
		[Display(Name = "Data Inicial de Inscrição")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataInicioInscricao { get; set; }

		[Required]
		[Display(Name = "Data Final de Inscrição")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataFimInscricao { get; set; }

		[Required]
		[Display(Name = "Valor da Inscrição", Prompt = "R$ 00.00")]
        [Range(0.00, double.MaxValue, ErrorMessage = "O valor da inscrição deve ser zero ou maior que zero.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Por favor, insira no máximo duas casas decimais e use '.' como separador decimal.")]
        public decimal ValorInscricao { get; set; }

		[Required]
		[Display(Name = "Há Certificação?")]
        public sbyte PossuiCertificado { get; set; }

		[Required]
		[Display(Name = "Frequência Minima")]
        public decimal FrequenciaMinimaCertificado { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "O número de vagas ofertadas deve ser um número inteiro positivo.")]
        public int VagasOfertadas { get; set; }

        [Display(Name = "Vagas Reservadas")]
        [Required(ErrorMessage = "Quantas vagas foram reservadas?")]
        public int VagasReservadas { get; set; }

        [Display(Name = "Vagas Disponíveis")]
        [Required(ErrorMessage = "Quantas vagas estão disponíveis?")]
        public int VagasDisponiveis { get; set; }

        [Display(Name = "Carga Horária")]
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "A carga horária deve ser um número inteiro positivo.")]
        public int CargaHoraria { get; set; }

        [Display(Name = "ID do Tipo do Evento")]
        [Required(ErrorMessage = "Informe qual o Tipo desse Subevento")]
        public uint IdTipoEvento { get; set; }

		[Required]
		[Display(Name = "Tipo de Subevento")]
        public string NomeTipoEvento { get; set; }

		[Required]
		[Display(Name = "Evento")]
        public string NomeEvento { get; set; }
    }

}

