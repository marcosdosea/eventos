using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class AreaInteresseModel
{
    [Display(Name = "Código")]
    [Required(ErrorMessage = "Código da área de interesse é obrigatório")]
    [Key]
    public uint  Id { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Nome da Área de Interesse é obrigatório")]
    [StringLength(100)]
    public string Nome { get; set; } = null!;
}