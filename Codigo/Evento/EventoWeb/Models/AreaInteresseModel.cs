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
	[StringLength(100, MinimumLength = 0, ErrorMessage = "O campo Nome deve ter 100 caracteres no máximo")]
	public string Nome { get; set; } = null!;
}