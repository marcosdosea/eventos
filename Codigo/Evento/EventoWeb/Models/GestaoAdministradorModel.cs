

using System.ComponentModel.DataAnnotations;
using Util;

namespace EventoWeb.Models
{
    public class GestaoAdministradorModel
    {
        public uint Id { get; set; }

        [Display(Name = "Nome")]
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Nome { get; set; } = null!;

        [Required(ErrorMessage = "O campo CPF é obrigatório.")]
        [CPF(ErrorMessage = "CPF inválido")]
        [Display(Name = "CPF", Prompt = "Digite seu CPF")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "O campo CPF deve ter 11 caracteres")]
        public string Cpf { get; set; } = null!;

        [Display(Name = "Telefone")]
        [TelefoneCelular(ErrorMessage = "Digite um telefone válido com DDD. Ex: (11) 91234-5678")]
        public string? Telefone1 { get; set; }

        [Display(Name = "E-mail")]
        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "Por favor, digite um e-mail em um formato válido.")]
        public string Email { get; set; } = null!;
        public List<PessoaModel> Administradores { get; set; } = new List<PessoaModel>();
    }
}
