using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Util;

namespace EventoWeb.Models
{
    public class ModelocrachaModel
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = "Código do Modelo de Crachá é obrigatório")]
        [Key]
        public uint Id { get; set; }

        [Display(Name = "Logotipo")]
        [Required(ErrorMessage = "Informe a logotipo")]
        [ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou GIF e ter menos de 1 MB.")]

        public IFormFile Logotipo { get; set; } = null!;

        [Display(Name = "Logotipo")]
        [BindNever]
        public string? LogotipoBase64 { get; set; }

        [Display(Name = "Texto")]
        [Required(ErrorMessage = "Informe o texto do crachá")]
        public string Texto { get; set; } = null!;

        [Display(Name = "Qrcode")]
        public sbyte Qrcode { get; set; }

        [Display(Name = "Evento")]
        [Required(ErrorMessage = "Informe qual o Evento")]
        public uint IdEvento { get; set; }

        public string? NomeEvento { get; set; }

        [Display(Name = "QR Code")]
        public string? QrCodeBase64 { get; set; }

        [Display(Name = "QR Codes")]
        public List<string>? QrCodes { get; set; } = new List<string>();

        public List<string>? Inscricoes { get; set; } = new List<string>();
    }
}
