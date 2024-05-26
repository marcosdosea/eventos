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
    

        public string Descricao { get; set; } = null!;

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

        [Display(Name = "Data de Inicio de Inscrição")]
        [Required(ErrorMessage = "Data de Inicio de Inscrição do Evento é obrigatório")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataFimInscricao { get; set; }

        public decimal ValorInscricao { get; set; }

        public string? Website { get; set; }

        public string? EmailEvento { get; set; }

        public sbyte EventoPublico { get; set; }

        public string Cep { get; set; } = null!;

        public string Estado { get; set; } = null!;

        public string Cidade { get; set; } = null!;

        public string Bairro { get; set; } = null!;

        public string Rua { get; set; } = null!;

        public string? Numero { get; set; }

        public string? Complemento { get; set; }

        public sbyte PossuiCertificado { get; set; }

        public decimal FrequenciaMinimaCertificado { get; set; }

        public int IdTipoEvento { get; set; }

        public int VagasOfertadas { get; set; }

        public int VagasReservadas { get; set; }

        public int VagasDisponiveis { get; set; }

        public int TempoMinutosReserva { get; set; }

        public int CargaHoraria { get; set; }
    }
}
