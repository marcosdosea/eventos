using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models;

public class GerenciarEventoModel
{
    public EventoModel Evento { get; set; }
    
    public IEnumerable<SubeventoEventoDTO> Subeventos { get; set; }

    public int NotificacoesEnviadas { get; set; }

    public int NotificacoesComFalha { get; set; }
}