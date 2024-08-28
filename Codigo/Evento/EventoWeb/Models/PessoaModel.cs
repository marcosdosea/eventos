using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Util;
namespace EventoWeb.Models;

public class PessoaModel
{
    [Display(Name = "C�digo")]
    [Required(ErrorMessage = "Código é obrigatório")]
    [Key]
    public uint Id { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Nome é obrigatério")]
    public string Nome { get; set; } = null!;

    [Display(Name = "Nome no Crachá")]
    [Required(ErrorMessage = "Informe o Nome para o crachá do evento")]
    public string NomeCracha { get; set; } = null!;

    [Required(ErrorMessage = "O campo CPF é obrigatório.")]
    [CPF(ErrorMessage = "CPF inválido")]
    [Display(Name = "CPF", Prompt = "Digite seu CPF")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "O campo CPF deve ter 11 caracteres")]
    public string Cpf { get; set; } = null!;

    [Display(Name = "Sexo")]
    [Required(ErrorMessage = "Informe o sexo")]
    public string Sexo { get; set; } = null!;

    [Display(Name = "CEP", Prompt = "00000-000")]
    [Cep(ErrorMessage = "CEP Inválido")]
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

    [Display(Name = "Numero", Prompt = "Sem n�mero, deixe o campo vazio")]
    public string? Numero { get; set; }

    [Display(Name = "Complemento")]
    public string? Complemento { get; set; }

    [Display(Name = "e-mail")]
    public string Email { get; set; } = null!;

    [Display(Name = "Telefone")]
    [TelefoneCelular(ErrorMessage = "Digite um número váliso")]
    public string? Telefone1 { get; set; }

    [Display(Name = "Telefone")]
    [TelefoneCelular(ErrorMessage = "Digite um número váliso")]
    public string? Telefone2 { get; set; }

    [Display(Name = "Foto")]
    [ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou GIF e ter menos de 1 MB.")]
    public IFormFile? Foto { get; set; }
    
    [Display(Name = "Foto")]
    [BindNever]
    public string? FotoBase64 { get; set; }
    
    public SelectList? Estados { get; set; }
}