using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class AreainteresseDTO
    {
        public uint Id { get; set; }
        public string Nome { get; set; } = null!;
    
        public virtual ICollection<Pessoaareainteresse> Pessoaareainteresses { get; set; } = new List<Pessoaareainteresse>();

        public virtual ICollection<Evento> IdEventos { get; set; } = new List<Evento>();
    }
}
