#nullable enable
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class AtualizarNomeCrachaModel
    {
        [Required(ErrorMessage = "O identificador do evento é obrigatório.")]
        public uint IdEvento { get; set; }

        [Required(ErrorMessage = "O identificador do participante é obrigatório.")]
        public uint IdPessoa { get; set; }

        [Required(ErrorMessage = "O nome completo do participante é obrigatório.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome completo deve ter entre 2 e 100 caracteres.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "O nome no crachá deve ter no máximo 20 caracteres.")]
        [Display(Name = "Nome no Crachá")]
        public string? NomeCracha { get; set; }
    }
}

