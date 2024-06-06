using System.ComponentModel.DataAnnotations;
using Core;

namespace EventoWeb.Models;

public class GestorEventoModel
{
    public uint Id { get; set; }
    
    [Required(ErrorMessage = "Campo requerido")]
    public uint IdEvento { get; set; }
    
    [Required(ErrorMessage = "Campo requerido")]
    public string Nome { get; set; } = null!;
    
    [Required(ErrorMessage = "Campo requerido")]
    public string NomeCracha { get; set; } = null!;
    
    [Required(ErrorMessage = "Campo requerido")]
    public string Cpf { get; set; } = null!;
    
    [Required(ErrorMessage = "Campo requerido")]
    public string Sexo { get; set; } = null!;
    
    public string? Telefone1 { get; set; }
    
    [Required(ErrorMessage = "Campo requerido")]
    public string Email { get; set; } = null!;
    
}