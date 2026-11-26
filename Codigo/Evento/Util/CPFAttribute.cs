using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Util
{
	/// <summary>
	/// Validação customizada para CPF
	/// </summary>
	public class CPFAttribute : ValidationAttribute
	{
		public override bool IsValid(object? value)
		{
			if (value == null || string.IsNullOrEmpty(value.ToString()))
				return true;
			return value is string cpf && Methods.ValidarCpf(cpf);
		}

		public string GetErrorMessage() =>
			$"CPF Inválido";
	}
}
