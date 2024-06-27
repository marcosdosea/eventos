using System;
using System.Collections.Generic;

namespace Core;

public partial class Inscricaopessoaevento
{
    public uint IdPessoa { get; set; }

    public uint IdEvento { get; set; }

    public uint IdPapel { get; set; }

    public uint IdTipoInscricao { get; set; }

    public DateTime DataInscricao { get; set; }

    public decimal ValorTotal { get; set; }

    /// <summary>
    /// A - ATIVA\nC - CANCELADA\nS - SOLICITADA\n
    /// </summary>
    public string Status { get; set; } = null!;

    public decimal FrequenciaFinal { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Papel IdPapelNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;

    public virtual Tipoinscricao IdTipoInscricaoNavigation { get; set; } = null!;
}
