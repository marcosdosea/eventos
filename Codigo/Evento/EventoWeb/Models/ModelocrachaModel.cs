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
        [ImagemUpload]
        public IFormFile Logotipo { get; set; } = null!;

		[Display(Name = "Logotipo")]
        [BindNever]
        public string? LogotipoBase64 { get; set; }

        [Display(Name = "Texto")]
        [Required(ErrorMessage = "Informe o texto do crachá")]
		[StringLength(200, MinimumLength = 0, ErrorMessage = "O campo Texto deve ter no máximo 200 caracteres")]
		public string Texto { get; set; } = null!;

		[Required]
		[Display(Name = "Qrcode")]
        public sbyte Qrcode { get; set; }

        [Display(Name = "Evento")]
        [Required(ErrorMessage = "Informe qual o Evento")]
        public uint IdEvento { get; set; }
<<<<<<< HEAD
		public string NomeEvento { get; set; }
=======
		[Required]
        public string? NomeEvento { get; set; }

        [Display(Name = "QR Code")]
        public string? QrCodeBase64 { get; set; }
>>>>>>> d914ce4fdb0719d3399c11989d3c2815d0556d51
    }
}
