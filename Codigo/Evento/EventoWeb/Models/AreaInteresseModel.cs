using System.Collections;
using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class AreaInteresseModel
{
    [Required(ErrorMessage = "Campo requerido")]
    public uint  Id { get; set; }
    
    [Required(ErrorMessage = "Campo requerido")]
    [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = null!;
}