using System.ComponentModel.DataAnnotations;

namespace EventoApi.Models;

public class BipouSessaoModel
{
    [Required, StringLength(256)]
    public string Usuario { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string Senha { get; set; } = string.Empty;
}

