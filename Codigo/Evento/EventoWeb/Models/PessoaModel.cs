using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class PessoaModel
{
    public uint Id { get; set; }

    [Required(ErrorMessage = "Campo requerido")]
    public string Nome { get; set; } = null!;
    
    [Required(ErrorMessage = "Campo requerido")]
    public string NomeCracha { get; set; } = null!;
    
    [Required(ErrorMessage = "Campo requerido")]
    public string Cpf { get; set; } = null!;
    
    [Required(ErrorMessage = "Campo requerido")]
    public string Sexo { get; set; } = null!;

    public string? Cep { get; set; }

    public string? Rua { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? Estado { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    [Required(ErrorMessage = "Campo requerido")]
    public string Email { get; set; } = null!;

    public string? Telefone1 { get; set; }

    public string? Telefone2 { get; set; }
}