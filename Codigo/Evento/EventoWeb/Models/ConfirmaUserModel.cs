using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace EventoWeb.Models
{
    public class ConfirmaUserModel
    {
        public string Email { get; set; } = null!;

        public string Cpf { get; set; } = null!;
    }
}
