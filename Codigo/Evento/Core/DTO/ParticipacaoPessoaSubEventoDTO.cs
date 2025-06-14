using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Core.DTO
{
    public class ParticipacaoPessoaSubEventoDTO
    {
        public uint Id { get; set; }
        public uint IdPessoa { get; set; }
        public uint IdSubEvento { get; set; }
        public DateTime Entrada { get; set; }
        public DateTime? Saida { get; set; }
        public string NomePessoa { get; set; } = null!;
        public string NomeSubEvento { get; set; } = null!;
    }
}
