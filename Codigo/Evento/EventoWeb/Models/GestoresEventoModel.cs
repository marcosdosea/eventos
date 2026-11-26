using System.ComponentModel.DataAnnotations;
using Core;
using Core.DTO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Util;

namespace EventoWeb.Models;

public class GestoresEventoModel
{
    public uint IdEvento { get; set; }
    public GestorEventoForm Pessoa { get; set; } = new();

    [BindNever, ValidateNever]
    public EventoSimpleDTO Evento { get; set; } = null!;

    [BindNever, ValidateNever]
    public IReadOnlyList<Inscricaopessoaevento> Gestores { get; set; } = [];
}

public class GestorEventoForm
{
    [Required(ErrorMessage = "O campo CPF é obrigatório.")]
    [CPF(ErrorMessage = "CPF inválido.")]
    public string Cpf { get; set; } = null!;

    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(50, ErrorMessage = "O nome não pode ter mais de 50 caracteres.")]
    public string Nome { get; set; } = null!;

    [TelefoneCelular(ErrorMessage = "Digite um número de celular válido com DDD.")]
    public string? Telefone1 { get; set; }

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Digite um e-mail válido.")]
    public string Email { get; set; } = null!;
}
