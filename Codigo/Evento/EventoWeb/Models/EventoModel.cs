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

        [Display(Name = "Descrição")]
        [Required(ErrorMessage = "Fale um pouco a respeito desse Evento")]
        public string Descricao { get; set; } = null!;

        [Display(Name = "Inscrição Gratuita")]
        [Required(ErrorMessage = "Informe se a Inscrição é Gratuita")]
        public sbyte InscricaoGratuita { get; set; }

        /// <summary>
        /// C- CADASTRO
        /// A- ATIVO
        /// I - INATIVO
        /// F- FINALIZADO
        ///  
        /// </summary>
        [Display(Name = "Status")]
        [Required(ErrorMessage = "Status do Evento é obrigatório")]
        public string Status { get; set; } = null!;

        [Display(Name = "Data de Inicio de Inscrição")]
        [Required(ErrorMessage = "Data de Inicio de Inscrição do Evento é obrigatório")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataInicioInscricao { get; set; }

        [Display(Name = "Data Final de Inscrição")]
        [Required(ErrorMessage = "Data Final de Inscrição do Evento é obrigatório")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataFimInscricao { get; set; }

        [Display(Name = "Valor da Inscrição")]
        [Required(ErrorMessage = "Valor da Inscrição do Evento é obrigatório")]
        public decimal ValorInscricao { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "e-mail")]
        public string? EmailEvento { get; set; }

        [Display(Name = "Evento Publico")]
        [Required(ErrorMessage = "Infome se o Evento é Publico")]
        public sbyte EventoPublico { get; set; }

        [Display(Name = "CEP")]
        [Required(ErrorMessage = "O CEP é obrigatório ")]
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
        [Required(ErrorMessage = "Informe o Numero onde o Evento será realizado")]
        public string? Numero { get; set; }

        [Display(Name = "Complemento")]
        public string? Complemento { get; set; }

        [Display(Name = "Há Certificação?")]
        [Required(ErrorMessage = "Informe se o Evento possuirá Certificado")]
        public sbyte PossuiCertificado { get; set; }

        [Display(Name = "Frequência Minima para Receber a Certificação")]
        [Required(ErrorMessage = "Informe a Frequência Minima")]
        public decimal FrequenciaMinimaCertificado { get; set; }

        [Display(Name = "ID do Tipo do Evento")]
        [Required(ErrorMessage = "Informe qual o Tipo desse Evento")]
        public int IdTipoEvento { get; set; }

        [Display(Name = "Vagas Ofertadas")]
        [Required(ErrorMessage = "Informe a quantidade de Vagas Ofertadas pra esse evento")]
        public int VagasOfertadas { get; set; }

        [Display(Name = "Vagas Reservadas")]
        [Required(ErrorMessage = "Quantas vagas foram reservadas?")]
        public int VagasReservadas { get; set; }

        [Display(Name = "Vagas Disponíveis")]
        [Required(ErrorMessage = "Quantas vagas estão disponíveis?")]
        public int VagasDisponiveis { get; set; }

        [Display(Name = "Tempo de Reserva em Minutos")]
        [Required(ErrorMessage = "Informe o Tempo da Reserva de uma Vaga")]
        public int TempoMinutosReserva { get; set; }

        [Display(Name = "Carga Horária")]
        [Required(ErrorMessage = "Informe a Carga Horária do Evento")]
        public int CargaHoraria { get; set; }
    }
}
