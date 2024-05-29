using Core;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class ModelocrachaModel
    {
        
        [Key]
        public int Id { get; set; }

        [Display(Name = "Logotipo do crachá ")]
        [Required(ErrorMessage = "Campo requerido")]
        public byte[] Logotipo { get; set; } = null!;

        [Display(Name = "Texto do crachá ")]
        [Required(ErrorMessage = "Campo requerido")]
        public string Texto { get; set; } = null!;

        [Display(Name = "Qrcode do crachá ")]
        public sbyte Qrcode { get; set; }

        public uint IdEvento { get; set; }

        public virtual Evento IdEventoNavigation { get; set; } = null!;
    }
}
