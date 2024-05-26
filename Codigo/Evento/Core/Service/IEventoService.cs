using Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Core.Service
{
    public interface IEventoService
    {
        public uint Inserir(Evento evento);
        public void Atualizar(Evento evento);
        public void Remover(uint Id);

        public Evento Obter(uint Id);

        public IEnumerable<Evento> GetAll();

        public IEnumerable<Evento> GetByNome(string Nome);
    }
}
