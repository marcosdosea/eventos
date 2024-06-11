using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class AreaInteresseModel
{
    [Display(Name = "Código")]
    [Required(ErrorMessage = "Código do Evento é obrigatório")]
    [Key]
    public uint  Id { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Nome do Evento é obrigatório")]
    public string Nome { get; set; } = null!;
}