using System;

namespace Core;

public partial class Notificacaoevento
{
    public const string StatusPendente = "P";
    public const string StatusEnviado = "E";
    public const string StatusFalha = "F";
    public const string StatusCancelado = "C";

    public uint Id { get; set; }

    public uint IdEvento { get; set; }

    public uint IdPessoa { get; set; }

    public string EmailDestino { get; set; } = null!;

    /// <summary>
    /// P - PENDENTE
    /// E - ENVIADO
    /// F - FALHA
    /// C - CANCELADO
    /// </summary>
    public string Status { get; set; } = StatusPendente;

    public int Tentativas { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime? DataUltimaTentativa { get; set; }

    public DateTime? DataEnvio { get; set; }

    public string? MensagemErro { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;
}
