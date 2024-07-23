using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;


namespace Util
{
	/// <summary>
	/// Validação customizada para Imagens upload do sistema
	/// </summary>
	public class ImagemUpload : ValidationAttribute { 
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

            var imagem = (IFormFile) value;
            if (imagem.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    imagem.CopyTo(memoryStream);
                    // Upload the file if less than 1 MB				
                    return memoryStream.Length < 1046026;
                }
            }



            var valueNoEspecial = Methods.RemoveSpecialsCaracts((string)value);
			if (valueNoEspecial.ToString().Length != 8)
				return false;
			if (valueNoEspecial.ToString().StartsWith('0'))
				return false;
			return true;
		}

        public string GetErrorMessage() =>
			$"Formato de Imagem Inválido";
	}
}
