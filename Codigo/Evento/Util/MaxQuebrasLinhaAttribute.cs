using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Util
{
    public class MaxQuebrasLinhaAttribute : ValidationAttribute
    {
        private readonly int _maxQuebras;

        public MaxQuebrasLinhaAttribute(int maxQuebras)
        {
            _maxQuebras = maxQuebras;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string texto)
            {
                var quebras = texto.Count(c => c == '\n');
                if (quebras > _maxQuebras)
                {
                    return new ValidationResult(ErrorMessage ?? $"O texto do crachá deve ter no máximo {_maxQuebras} quebras de linha.");
                }
            }
            return ValidationResult.Success;
        }
    }
}
