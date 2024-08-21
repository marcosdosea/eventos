using Core;
using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models;

public class TipoInscricaoSubeventoModel
{
    public SubeventoModel Subevento { get; set; } = null!;
    public SelectList TiposInscricaos { get; set; }
    public IEnumerable<TipoInscricaoDTO> TiposInscricaosSubevento { get; set; }
    public uint IdTipoInscricao { get; set; }
}
