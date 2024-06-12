using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class ModelocrachaModel
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = "Código do Modelo de Crachá é obrigatório")]
        [Key]
        public int Id { get; set; }

        [Display(Name = "Logotipo do crachá ")]
        [Required(ErrorMessage = "Informe a logotipo")]
        public byte[] Logotipo { get; set; } = null!;

        [Display(Name = "Texto do crachá ")]
        [Required(ErrorMessage = "Informe o texto do crachá")]
        public string Texto { get; set; } = null!;

        [Display(Name = "Qrcode do crachá ")]
        public sbyte Qrcode { get; set; }

        [Display(Name = "ID do Evento")]
        [Required(ErrorMessage = "Informe qual o Evento")]
        public uint IdEvento { get; set; }
    }
}
