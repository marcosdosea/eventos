namespace Core.DTO;

public class DestinatarioNotificacaoEventoDTO
{
    public uint IdPessoa { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<string> AreasInteresseCorrespondentes { get; set; } = Array.Empty<string>();
}

public class ResultadoNotificacaoEventoDTO
{
    public uint IdEvento { get; set; }

    public bool EventoElegivel { get; set; }

    public bool JaDivulgadoAnteriormente { get; set; }

    public int TotalDestinatariosElegiveis { get; set; }

    public int Enviados { get; set; }

    public int Falhas { get; set; }

    public int IgnoradosJaEnviados { get; set; }

    public int CanceladosPorPreferenciaAlterada { get; set; }

    public List<string> Erros { get; set; } = new();
}

public class ResumoNotificacaoEventoDTO
{
    public int Enviados { get; set; }

    public int Falhas { get; set; }

    public int Pendentes { get; set; }

    public DateTime? UltimaTentativa { get; set; }
}
