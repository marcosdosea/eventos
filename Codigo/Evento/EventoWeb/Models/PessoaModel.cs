using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class PessoaModel
{
    [Display(Name = "C�digo")]
    [Required(ErrorMessage = "C�digo � obrigat�rio")]
    [Key]
    public uint Id { get; set; }

	[StringLength(50, MinimumLength = 0, ErrorMessage = "O campo Nome deve ter m�ximo 50 caracteres")]
	[Display(Name = "Nome")]
    [Required(ErrorMessage = "Nome � obrigat�rio")]
    public string Nome { get; set; } = null!;

	[StringLength(20, MinimumLength = 0, ErrorMessage = "O campo NomeCracha deve ter no m�ximo 20 caracteres")]
	[Display(Name = "Nome no Crach�")]
    [Required(ErrorMessage = "Informe o Nome para o crach� do evento")]
    public string NomeCracha { get; set; } = null!;

	[Required]
	[Display(Name = "CPF", Prompt = "000.000.000-00")]
    [RegularExpression(@"^\d{3}.\d{3}.\d{3}-\d{2}$", ErrorMessage = "O CPF deve estar no formato 000.000.000-00.")]
    [StringLength(14, MinimumLength = 14, ErrorMessage = "O campo CPF deve ter 11 caracteres")]
    public string Cpf { get; set; } = null!;

    [Display(Name = "Sexo")]
    [Required(ErrorMessage = "Informe o sexo")]
    public string Sexo { get; set; } = null!;

	[Display(Name = "CEP", Prompt = "00000-000")]
    [RegularExpression(@"^\d{5}-\d{3}$", ErrorMessage = "O CEP deve estar no formato 00000-000.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "O campo CEP deve ter 8 caracteres")]
    public string Cep { get; set; } = null!;

	[StringLength(2, MinimumLength = 2, ErrorMessage = "Insira um Estado v�lido")]
	[Display(Name = "Estado")]
    public string Estado { get; set; } = null!;

	[StringLength(50, MinimumLength = 0, ErrorMessage = "O campo Cidade deve ter 50 caracteres no m�ximo")]
	[Display(Name = "Cidade")]
    public string Cidade { get; set; } = null!;

	[StringLength(50, MinimumLength = 0, ErrorMessage = "O campo Bairro deve ter 50 caracteres no m�ximo")]
	[Display(Name = "Bairro")]
    public string Bairro { get; set; } = null!;

	[StringLength(50, MinimumLength = 0, ErrorMessage = "O campo Rua deve ter 50 caracteres no m�ximo")]
	[Display(Name = "Rua")]
    public string Rua { get; set; } = null!;

	[StringLength(10, MinimumLength = 0, ErrorMessage = "O campo Numero deve ter 10 caracteres no m�ximo")]
	[Display(Name = "Numero", Prompt = "Sem n�mero, deixe o campo vazio")]
    public string? Numero { get; set; }

	[StringLength(50, MinimumLength = 0, ErrorMessage = "O campo Complemento deve ter 50 caracteres no m�ximo")]
	[Display(Name = "Complemento")]
    public string? Complemento { get; set; }

    [Required]
	[StringLength(50, MinimumLength = 15, ErrorMessage = "O campo Email deve ter 12 caracteres no m�ximo")]
	[Display(Name = "e-mail")]
    public string Email { get; set; } = null!;

	[StringLength(12, MinimumLength = 12, ErrorMessage = "O campo telefone1 deve ter 12 digitos no m�ximo")]
	[Display(Name = "Telefone")]
    public string? Telefone1 { get; set; }

	[StringLength(12, MinimumLength = 12, ErrorMessage = "O campo telefone2 deve ter 12 digitos no m�ximo")]
	[Display(Name = "Telefone")]
    public string? Telefone2 { get; set; }
}