using Core;
using Core.DTO;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models;

public class GestaoPapelModel
{
	[Required]
	public Pessoa Pessoa { get; set; }

	[Required]
	public EventoSimpleDTO Evento { get; set; }
    public IEnumerable<Inscricaopessoaevento> Inscricoes { get; set; }

	[Required]
	public int IdPapel { get; set; }
}