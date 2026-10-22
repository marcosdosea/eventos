using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Util;
using Core.DTO;

namespace EventoWeb.Models
{
    public class ModelocrachaModel
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = "Código do Modelo de Crachá é obrigatório")]
        [Key]
        public uint Id { get; set; }

        [Display(Name = "Logotipo")]
        [ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou WEBP e ter no máximo 64 KB.")]
        public IFormFile? Logotipo { get; set; }

        [Display(Name = "Logotipo")]
        public string? LogotipoBase64 { get; set; }

        public string? LogotipoDataUrl
        {
            get
            {
                if (string.IsNullOrEmpty(LogotipoBase64))
                    return null;

                if (LogotipoBase64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                    return LogotipoBase64;

                string? mime = ObterMimePorBase64(LogotipoBase64);
                if (mime == null && !string.IsNullOrEmpty(NomeArquivo))
                {
                    var ext = Path.GetExtension(NomeArquivo).ToLowerInvariant();
                    mime = ext switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".webp" => "image/webp",
                        ".svg" => "image/svg+xml",
                        ".tif" or ".tiff" => "image/tiff",
                        ".gif" => "image/gif",
                        _ => "image/png"
                    };
                }

                return $"data:{mime ?? "image/png"};base64,{LogotipoBase64}";
            }
        }

        private static string? ObterMimePorBase64(string base64)
        {
            if (base64.StartsWith("/9j/", StringComparison.Ordinal))
                return "image/jpeg";
            if (base64.StartsWith("iVBORw0KGgo", StringComparison.Ordinal))
                return "image/png";
            if (base64.StartsWith("R0lGOD", StringComparison.Ordinal))
                return "image/gif";
            if (base64.StartsWith("UklGR", StringComparison.Ordinal))
                return "image/webp";
            if (base64.StartsWith("SUkq", StringComparison.Ordinal) ||
                base64.StartsWith("TU0A", StringComparison.Ordinal))
                return "image/tiff";
            if (base64.StartsWith("PHN2Zy", StringComparison.Ordinal) ||
                base64.StartsWith("PD94bWw", StringComparison.Ordinal))
                return "image/svg+xml";

            return null;
        }

        private string _texto = string.Empty;

        [Display(Name = "Texto")]
        [Required(ErrorMessage = "Informe o texto do crachá")]
        [StringLength(200, ErrorMessage = "O texto do crachá deve ter no máximo 200 caracteres")]
        [MaxQuebrasLinha(5, ErrorMessage = "O texto do crachá deve ter no máximo 5 quebras de linha")]
        public string Texto
        {
            get => _texto;
            set => _texto = value != null ? value.Replace("\r\n", "\n") : null!;
        }

        [Display(Name = "Qrcode")]
        public sbyte Qrcode { get; set; }

        [Display(Name = "Evento")]
        [Required(ErrorMessage = "Informe qual o Evento")]
        public uint IdEvento { get; set; }

        public string? NomeEvento { get; set; }

        [Display(Name = "QR Code")]
        public string? QrCodeBase64 { get; set; }

		public uint? IdPessoa { get; set; }

		[Display(Name = "QR Codes")]
        public List<string>? QrCodes { get; set; } = new List<string>();

        public List<string>? Inscricoes { get; set; } = new List<string>();

        [Display(Name = "Crachás")]
        public List<CrachaParticipanteModel> Crachas { get; set; } = new List<CrachaParticipanteModel>();
        
        public EventoSimpleDTO? Evento { get; set; }

        public string? NomeArquivo { get; set; }

        public string? TamanhoArquivo { get; set; }
    }

    public class CrachaParticipanteModel
    {
        public uint IdPessoa { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string NomeCracha { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
    }
}
