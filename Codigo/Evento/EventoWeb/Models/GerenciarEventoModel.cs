using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models;

public class GerenciarEventoModel
{
	[Required]
	public EventoModel Evento { get; set; }

	[Required]
	public IEnumerable<SubeventoEventoDTO> Subeventos { get; set; }
}