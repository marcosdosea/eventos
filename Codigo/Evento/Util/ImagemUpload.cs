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
            if (fileBytes == null || fileBytes.Length < 3)
                return false;

            if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
                return true;

            if (fileBytes.Length >= 12 &&
                fileBytes[0] == 0x52 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x46 &&
                fileBytes[8] == 0x57 && fileBytes[9] == 0x45 && fileBytes[10] == 0x42 && fileBytes[11] == 0x50)
            {
                return true;
            }

            var headers = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("GIF"),
                new byte[] { 137, 80, 78, 71 },
                new byte[] { 73, 73, 42 },
                new byte[] { 77, 77, 42 }
            };

            return headers.Any(header => fileBytes.Take(header.Length).SequenceEqual(header));
        }
    }
}
