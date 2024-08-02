using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class PessoaModel
{
    [Display(Name = "Código")]
    [Required(ErrorMessage = "Código é obrigatório")]
    [Key]
    public uint Id { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Nome é obrigatório")]
    public string Nome { get; set; } = null!;

    [Display(Name = "Nome no Crachá")]
    [Required(ErrorMessage = "Informe o Nome para o crachá do evento")]
    public string NomeCracha { get; set; } = null!;

	[Required]
	[Display(Name = "CPF", Prompt = "000.000.000-00")]
    [RegularExpression(@"^\d{3}.\d{3}.\d{3}-\d{2}$", ErrorMessage = "O CPF deve estar no formato 000.000.000-00.")]
    [StringLength(14, MinimumLength = 14, ErrorMessage = "O campo CPF deve ter 11 caracteres")]
    public string Cpf { get; set; } = null!;


    [Display(Name = "Sexo")]
    [Required(ErrorMessage = "Informe o sexo")]
    public string Sexo { get; set; } = null!;

	[Required]
	[Display(Name = "CEP", Prompt = "00000-000")]
    [RegularExpression(@"^\d{5}-\d{3}$", ErrorMessage = "O CEP deve estar no formato 00000-000.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "O campo CEP deve ter 8 caracteres")]
    public string Cep { get; set; } = null!;

    [Display(Name = "Estado")]
    [Required(ErrorMessage = "Informe o Estado onde o Evento será realizado")]
    public string Estado { get; set; } = null!;

    [Display(Name = "Cidade")]
    [Required(ErrorMessage = "Informe a Cidade onde o Evento será realizado")]
    public string Cidade { get; set; } = null!;

    [Display(Name = "Bairro")]
    [Required(ErrorMessage = "Informe o Bairro onde o Evento será realizado")]
    public string Bairro { get; set; } = null!;

    [Display(Name = "Rua")]
    [Required(ErrorMessage = "Informe a Rua onde o Evento será realizado")]
    public string Rua { get; set; } = null!;

	[Display(Name = "Numero", Prompt = "Sem número, deixe o campo vazio")]
    public string? Numero { get; set; }

    [Display(Name = "Complemento")]
    public string? Complemento { get; set; }

    [Display(Name = "e-mail")]
    public string Email { get; set; } = null!;

    [Display(Name = "Telefone")]
    public string? Telefone1 { get; set; }
    [Display(Name = "Telefone")]
    public string? Telefone2 { get; set; }
}