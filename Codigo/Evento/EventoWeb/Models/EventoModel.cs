using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class EventoModel
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = "Código do Evento é obrigatório")]
        [Key]
        public uint Id { get; set; }

        [Display(Name = "Nome")]
        [Required(ErrorMessage = "Nome do Evento é obrigatório")]
        
        public string Nome { get; set; } = null!;
        [Display(Name = "Data Inicial do Evento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        
        public DateTime DataInicio { get; set; }
        [Display(Name = "Data Final do Evento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataFim { get; set; }

        [Display(Name = "Descrição")]
        [Required(ErrorMessage = "Fale um pouco a respeito desse Evento")]
        public string Descricao { get; set; } = null!;

        [Display(Name = "Inscrição Gratuita")]
        public sbyte InscricaoGratuita { get; set; }

        [Display(Name = "Status")]
        [Required(ErrorMessage = "Status do Evento é obrigatório")]
        public string Status { get; set; } = null!;


        [Display(Name = "Data de Inicio de Inscrição")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataInicioInscricao { get; set; }

        [Display(Name = "Data Final de Inscrição")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataFimInscricao { get; set; }

        [Display(Name = "Valor da Inscrição", Prompt = "R$ 00.00")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da inscrição deve ser maior que zero.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Por favor, insira no máximo duas casas decimais e use '.' como separador decimal.")]
        public decimal ValorInscricao { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "e-mail")]
        public string? EmailEvento { get; set; }

        [Display(Name = "Evento Publico")]
        public sbyte EventoPublico { get; set; }

        [Display(Name = "CEP")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "O CEP deve estar no formato 00000000.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "O campo CEP deve ter 8 caracteres")]
        public string Cep { get; set; } = null!;

        [Display(Name = "Estado")]
        [Required(ErrorMessage = "Informe o Estado onde o Evento será realizado")]
        public string Estado { get; set; } = null!;

        [Display(Name = "Cidade")]
        [Required(ErrorMessage = "Informe a Cidade onde o Evento será realizado")]
        public string Cidade { get; set; } = null!;

        [Display(Name = "Bairro")]
        [Required(ErrorMessage = "Informe o Bairro onde o Evento será realizado")]
        public string Bairro { get; set; } = null!;

        [Display(Name = "Rua")]
        [Required(ErrorMessage = "Informe a Rua onde o Evento será realizado")]
        public string Rua { get; set; } = null!;

        [Display(Name = "Numero")]
        public string? Numero { get; set; }

        [Display(Name = "Complemento")]
        public string? Complemento { get; set; }

        [Display(Name = "Há Certificação?")]
        public sbyte PossuiCertificado { get; set; }

        [Display(Name = "Frequência Minima para Receber a Certificação")]
        public decimal FrequenciaMinimaCertificado { get; set; }

        [Display(Name = "ID do Tipo do Evento")]
        [Required(ErrorMessage = "Informe qual o Tipo desse Evento")]
        public uint IdTipoEvento { get; set; }

        [Display(Name = "Vagas Ofertadas")]
        [Required(ErrorMessage = "Informe a quantidade de Vagas Ofertadas pra esse evento")]
        public int VagasOfertadas { get; set; }

        [Display(Name = "Vagas Reservadas")]
        [Required(ErrorMessage = "Quantas vagas foram reservadas?")]
        public int VagasReservadas { get; set; }

        [Display(Name = "Vagas Disponíveis")]
        [Required(ErrorMessage = "Quantas vagas estão disponíveis?")]
        public int VagasDisponiveis { get; set; }
		public string NomeTipoEvento { get; set; }

		[Display(Name = "Tempo de Reserva em Minutos")]
        [Required(ErrorMessage = "Informe o Tempo da Reserva de uma Vaga")]
        public int TempoMinutosReserva { get; set; }

        [Display(Name = "Carga Horária")]
        [Required(ErrorMessage = "Informe a Carga Horária do Evento")]
        public int CargaHoraria { get; set; }
    }
}
