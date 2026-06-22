using System.ComponentModel.DataAnnotations;
using Util;

namespace EventoWeb.Models
{
    public class GestorModel
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
        [TelefoneCelular(ErrorMessage = "Digite um número válido")]
        public string? Telefone1 { get; set; }

        [Display(Name = "E-mail")]
        [Required(ErrorMessage = "E-mail é obrigatório")]
        public string Email { get; set; } = null!;
        public List<PessoaModel> Gestores { get; set; } = new List<PessoaModel>();
    }
}
