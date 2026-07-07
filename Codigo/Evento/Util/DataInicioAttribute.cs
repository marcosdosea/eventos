using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Util
{
    public class DataInicioAttribute : ValidationAttribute
    {
        private string dataFin { get; set; }
        public DataInicioAttribute(string dataFim) {
            dataFin = dataFim;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return ValidationResult.Success;

            DateTime dataInicio = Convert.ToDateTime(value);

            if (validationContext.ObjectType.GetProperty(this.dataFin) == null)
            {
                return new ValidationResult("A propriedade de data final não foi encontrada.");
            }

            var dataFinalProperty = validationContext.ObjectType.GetProperty(dataFin);
            if (dataFinalProperty == null)
            {
                return new ValidationResult("A propriedade de data final não foi encontrada.");
            }
            DateTime dataFinal = Convert.ToDateTime(dataFinalProperty.GetValue(validationContext.ObjectInstance));

            if (DateTime.Compare(dataInicio, dataFinal) > 0)
            {
                return new ValidationResult("A data de início não pode ser posterior à data de fim.");
            }
            return ValidationResult.Success;
        }

        public string GetErrorMessage() =>
            $"Data de início inválida";
    }
}
