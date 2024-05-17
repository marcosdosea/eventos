using System;
using System.Collections.Generic;

namespace Core;

public partial class Inscricaopessoasubevento
{
    public uint IdPessoa { get; set; }

    public uint IdSubEvento { get; set; }

    public int IdPapel { get; set; }

    public int? IdTipoInscricaoSubEvento { get; set; }

    public DateTime DataInscricao { get; set; }

    public decimal Valor { get; set; }

    /// <summary>
    /// A - ATIVA
    /// C - CANCELADA
    /// S - SOLICITADA
    /// 
    /// </summary>
    public string Status { get; set; } = null!;

    public decimal FrequenciaFinal { get; set; }

    public virtual Papel IdPapelNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;

    public virtual Subevento IdSubEventoNavigation { get; set; } = null!;

    public virtual Tipoinscricaosubevento? IdTipoInscricaoSubEventoNavigation { get; set; }
}
