using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
   
    public class ModelocertificadoDTO
    {
        public uint Id { get; set; }
        public uint IdPessoa { get; set; }
        public uint IdEvento { get; set; }
        public DateTime DataEmissao { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NomePessoa { get; set; } = string.Empty;
        public string NomeEvento { get; set; } = string.Empty;
    }
}
