using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Util
{
    /// <summary>
    /// Validação customizada para Imagens upload do sistema
    /// </summary>
    public class ImagemUpload : ValidationAttribute
    {
        /// <summary>
        /// Construtor
        /// </summary>
        public ImagemUpload() { }

        /// <summary>
        /// Validação server
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            if (value is IFormFile imagem)
            {
                if (imagem.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        imagem.CopyTo(memoryStream);
                        if (memoryStream.Length < 65535)
                        {
                            byte[] bytes = memoryStream.ToArray();
                            if (!bytes.IsImagem())
                            {
                                ErrorMessage = "Formato de Imagem Inválido";
                                return false;
                            }
                            return true;
                        }
                        else
                        {
                            ErrorMessage = "O arquivo de imagem deve ter menos de 64 KB.";
                            return false;
                        }
                    }
                }
            }

            var valueNoEspecial = Methods.RemoveSpecialsCaracts(value.ToString());
            if (valueNoEspecial.Length != 8 || valueNoEspecial.StartsWith('0'))
                return false;

            return true;
        }
        public string GetErrorMessage() =>
            $"Formato de Imagem Inválido";
    }

    public static class ImageExtensions
    {
        public static bool IsImagem(this byte[] fileBytes)
        {
            var headers = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("GIF"),     // GIF
                new byte[] { 137, 80, 78, 71 },     // PNG
                new byte[] { 73, 73, 42 },          // TIFF
                new byte[] { 77, 77, 42 },          // TIFF
                new byte[] { 255, 216, 255, 224 },  // JPEG
                new byte[] { 255, 216, 255, 225 }   // JPEG CANON
            };

            return headers.Any(header => fileBytes.Take(header.Length).SequenceEqual(header));
        }
    }
}
