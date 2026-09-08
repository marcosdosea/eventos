namespace Core.DTO;

/// <summary>
/// Projeção leve para listagens de eventos (sem BLOBs como ImagemPortal).
/// </summary>
public class EventoListDTO
{
    public uint Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public DateTime? DataInicio { get; set; }

    public string Status { get; set; } = string.Empty;

    public uint IdTipoEvento { get; set; }

    public string NomeTipoEvento { get; set; } = string.Empty;
}
