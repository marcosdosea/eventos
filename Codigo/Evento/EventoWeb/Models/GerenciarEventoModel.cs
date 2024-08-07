using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models;

public class GerenciarEventoModel
{
	public EventoModel Evento { get; set; }

	
	public IEnumerable<SubeventoEventoDTO> Subeventos { get; set; }
}