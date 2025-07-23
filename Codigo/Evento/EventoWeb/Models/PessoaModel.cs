using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Util;

namespace EventoWeb.Models
{
    public class PessoaModel
    {
        public uint Id { get; set; }

        [Display(Name = "Nome")]
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Nome { get; set; } = null!;

        private string? _nomeCracha;

        [Display(Name = "Nome no Crachá")]
        [Required(ErrorMessage = "Informe o nome que será exibido no crachá do evento")]
        [StringLength(20, ErrorMessage = "O Nome no Crachá não pode exceder {1} caracteres.")] // <--- CORRIGIDO PARA 20
        public string NomeCracha
        {
            get => string.IsNullOrWhiteSpace(_nomeCracha) ? Nome : _nomeCracha;
            set => _nomeCracha = value;
        }

        [Required(ErrorMessage = "O campo CPF é obrigatório.")]
        [CPF(ErrorMessage = "CPF inválido")]
        [Display(Name = "CPF", Prompt = "Digite seu CPF")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "O campo CPF deve ter 11 caracteres")]
        public string Cpf { get; set; } = null!;

        [Display(Name = "Sexo")]
        public string? Sexo { get; set; } = null!;

        [Display(Name = "CEP", Prompt = "00000-000")]
        public string? Cep { get; set; } = null!;

        [Display(Name = "Estado")]
        public string? Estado { get; set; } = null!;

        [Display(Name = "Cidade")]
        public string? Cidade { get; set; } = null!;

        [Display(Name = "Bairro")]
        public string? Bairro { get; set; } = null!;

        [Display(Name = "Rua")]
        public string? Rua { get; set; } = null!;

        [Display(Name = "Número")]
        public string? Numero { get; set; }

        [Display(Name = "Complemento")]
        public string? Complemento { get; set; }

        [Display(Name = "E-mail")]
        [Required(ErrorMessage = "E-mail é obrigatório")]
        public string Email { get; set; } = null!;

        [Display(Name = "Telefone")]
        [TelefoneCelular(ErrorMessage = "Digite um número válido")]
        public string? Telefone1 { get; set; }

        [Display(Name = "Telefone")]
        public string? Telefone2 { get; set; }

        [Display(Name = "Foto")]
        [ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou GIF e ter menos de 1 MB.")]
        public IFormFile? Foto { get; set; }

        [Display(Name = "Foto")]
        [BindNever]
        public string? FotoBase64 { get; set; }

        public SelectList? Estados { get; set; }
    }
}
