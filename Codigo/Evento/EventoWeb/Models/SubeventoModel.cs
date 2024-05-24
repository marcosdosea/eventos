using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class SubeventoModel
    {
            [Key]
            public uint Id { get; set; }
            public uint IdEvento { get; set; }


            [Required(ErrorMessage = "Campo requerido")]

            [Display(Name = "Nome do subevento *")]

            public string Nome { get; set; } = null!;


            [Required(ErrorMessage = "Campo requerido")]

            [Display(Name = "Descrição do subevento *")]

            public string Descricao { get; set; } = null!;


            public sbyte InscricaoGratuita { get; set; }


            /// <summary>

            /// C- CADASTRO

            /// A- ABERTO

            /// F- FINALIZADO

            ///  

            /// </summary>

            [Required(ErrorMessage = "Campo requerido")]

            [Display(Name = "Status do subevento *")]

            public string Status { get; set; } = null!;


            public DateTime DataInicioInscricao { get; set; }
            public DateTime DataFimInscricao { get; set; }
            public decimal ValorInscricao { get; set; }
            public sbyte PossuiCertificado { get; set; }
            public decimal FrequenciaMinimaCertificado { get; set; }
            public int VagasOfertadas { get; set; }
            public int VagasReservadas { get; set; }
            public int VagasDisponiveis { get; set; }
            public int CargaHoraria { get; set; }
            public int IdTipoEvento { get; set; }
    }

}

