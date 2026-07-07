using Core;
using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models;

public class TipoInscricaoSubeventoModel
{
    public uint IdSubevento { get; set; }
    public string? NomeSubevento { get; set; }

    public SelectList? TiposInscricaos { get; set; }
    public IEnumerable<TipoInscricaoDTO>? TiposInscricaosSubevento { get; set; }

    public uint IdTipoInscricao { get; set; }

}
