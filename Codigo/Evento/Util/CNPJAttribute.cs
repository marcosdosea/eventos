﻿using System.ComponentModel.DataAnnotations;


namespace Util
{
    /// <summary>
    /// Validação customizada para CPF
    /// </summary>
    public class CNPJAttribute : ValidationAttribute
	{
		public override bool IsValid(object? value)
		{
			if (value == null || string.IsNullOrEmpty(value.ToString()))
				return true;
			var valueNoEspecial = Methods.RemoveSpecialsCaracts((string)value);
			bool valido = Methods.ValidarCnpj(valueNoEspecial.ToString());
			return valido;
		}

		public string GetErrorMessage() =>
			$"CNPJ Inválido";
	}
}
