using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class AreaInteresseModel
{
    [Display(Name = "Código")]
    [Required(ErrorMessage = "Código da Área de Interesse é obrigatório")]
    [Key]
    public uint  Id { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Nome da Área de Interesse é obrigatório")]
    public string Nome { get; set; } = null!;
}