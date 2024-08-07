using Core;
using Core.DTO;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models;

public class GestaoPapelModel
{
	public Pessoa Pessoa { get; set; }

	public EventoSimpleDTO Evento { get; set; }
    public IEnumerable<Inscricaopessoaevento> Inscricoes { get; set; }

	
	public int IdPapel { get; set; }
}