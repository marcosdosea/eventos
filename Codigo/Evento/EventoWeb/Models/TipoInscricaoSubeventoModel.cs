using Core;
using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models;

public class TipoInscricaoSubeventoModel
{
    public Subevento Subevento { get; set; } = null!;
    public SelectList TiposInscricaos { get; set; }
    public IEnumerable<TipoInscricaoDTO> TiposInscricaosSubevento { get; set; }
    
    [Required]
    public uint IdTipoInscricao { get; set; }
}
