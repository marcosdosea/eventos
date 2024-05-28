using Core;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class AreainteresseModel
    {
        [Key]
        public uint Id { get; set; }
        [Required(ErrorMessage = "Campo requerido")]

        [Display(Name = "Nome da area de interesse *")]
        public string Nome { get; set; } = null!;

        public virtual ICollection<Pessoaareainteresse> Pessoaareainteresses { get; set; } = new List<Pessoaareainteresse>();

        public virtual ICollection<Evento> IdEventos { get; set; } = new List<Evento>();
    }
}
