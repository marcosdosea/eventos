using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models
{
    public class TipoInscricaoModel
    {
        public int Id { get; set; }

        [Display(Name = "Evento")]
        public uint IdEvento { get; set; }

        [Display(Name = "Descrição")]
        [StringLength(200)]
        public string Descricao { get; set; } = null!;

        [Display(Name = "Valor do Tipo de Inscrição")]
        [Range(0, 9999999999999999.99)]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Valor { get; set; }

        [Display(Name = "Data de Inicio")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DataInicio { get; set; }

        [Display(Name = "Data de Inicio")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime Datafim { get; set; }


        [Display(Name = "Foi Usado no Evento?")]
        [Range(0, 1, ErrorMessage = "O valor deve ser Não ou Sim.")]
        public sbyte UsadaEvento { get; set; }

        [Display(Name = "Foi Usado no Subevento?")]
        [Range(0, 1, ErrorMessage = "O valor deve ser Não ou Sim.")]
        public sbyte UsadaSubevento { get; set; }
    }
}
