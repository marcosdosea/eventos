using Core;

namespace EventoWeb.Models;

public class GestorEventoModel
{
    public uint IdEvento { get; set; } 
    public Pessoa Pessoa { get; set; }
       
    public Inscricaopessoaevento Inscricao { get; set; }

    public IEnumerable<Inscricaopessoaevento> Inscricoes { get; set; }
    public IEnumerable<Evento> Eventos { get; set; }
    
    public IEnumerable<Pessoa> Gestores { get; set; }
    
  }