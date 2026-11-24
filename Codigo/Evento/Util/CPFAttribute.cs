using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Util
{
	/// <summary>
	/// Validação customizada para CPF
	/// </summary>
	public class CPFAttribute : ValidationAttribute
	{
		public CPFAttribute()
		{
			ErrorMessage = "CPF inválido";
		}

		public override bool IsValid(object? value)
		{
			if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
				return true;

			var texto = value.ToString()!.Trim();
			if (!Regex.IsMatch(texto, @"^[\d.\-\s]+$"))
				return false;

			return Methods.ValidarCpf(texto);
		}

		public string GetErrorMessage() =>
			$"CPF Inválido";
	}
}
