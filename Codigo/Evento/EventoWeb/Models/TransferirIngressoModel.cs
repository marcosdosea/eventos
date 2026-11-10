#nullable enable
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class TransferirIngressoModel
    {
        public uint? IdInscricao { get; set; }

        public uint? IdSubevento { get; set; }

        [Required(ErrorMessage = "O CPF do destinatário é obrigatório.")]
        [Display(Name = "CPF do Destinatário")]
        public string CpfAmigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome completo do destinatário é obrigatório.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome completo deve ter entre 2 e 50 caracteres.")]
        [Display(Name = "Nome Completo do Destinatário")]
        public string NomeAmigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail do destinatário é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(100, ErrorMessage = "O e-mail deve ter no máximo 100 caracteres.")]
        [Display(Name = "E-mail do Destinatário")]
        public string EmailAmigo { get; set; } = string.Empty;
    }
}
