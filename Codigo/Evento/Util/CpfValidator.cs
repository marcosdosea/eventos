using System.Text.RegularExpressions;

namespace Util
{
    /// <summary>
    /// Fornece métodos utilitários para validação e normalização de CPF.
    /// </summary>
    public static class CpfValidator
    {
        /// <summary>
        /// Normaliza o CPF removendo formatação, pontuação e espaços.
        /// Caso o CPF possua 10 dígitos (situação comum quando o Excel converte
        /// número e perde o zero à esquerda), tenta completar com zero à esquerda.
        /// </summary>
        /// <param name="cpf">Texto do CPF com ou sem formatação.</param>
        /// <returns>String contendo apenas os 11 dígitos numéricos do CPF, ou apenas dígitos limpos.</returns>
        public static string Normalizar(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return string.Empty;

            var apenasDigitos = Methods.RemoveNaoNumericos(cpf).Trim();

            if (apenasDigitos.Length == 10)
            {
                var comZero = apenasDigitos.PadLeft(11, '0');
                if (Validar(comZero))
                    return comZero;
            }

            return apenasDigitos;
        }

        /// <summary>
        /// Valida se o CPF informado possui formato válido, 11 dígitos,
        /// não é formado por dígitos repetidos e atende aos dígitos verificadores.
        /// </summary>
        /// <param name="cpf">Texto do CPF a validar.</param>
        /// <returns>True se o CPF for válido; caso contrário, False.</returns>
        public static bool Validar(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            var limpo = Methods.RemoveNaoNumericos(cpf).Trim();

            if (limpo.Length == 10)
            {
                var comZero = limpo.PadLeft(11, '0');
                if (Validar(comZero))
                    return true;
            }

            if (limpo.Length != 11)
                return false;

            // Rejeita sequências com todos os dígitos iguais (ex: 00000000000, 11111111111, etc.)
            if (new string(limpo[0], 11) == limpo)
                return false;

            return Methods.ValidarCpf(limpo);
        }

        /// <summary>
        /// Alias para Validar.
        /// </summary>
        public static bool IsValid(string? cpf) => Validar(cpf);
    }
}

