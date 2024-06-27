using System;
using System.Collections.Generic;

namespace Core;

public partial class Tipoinscricao
{
    public uint Id { get; set; }

    public uint IdEvento { get; set; }

    public string Nome { get; set; } = null!;

    public string Descricao { get; set; } = null!;

    public decimal Valor { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }

    public sbyte UsadaEvento { get; set; }

    public sbyte UsadaSubevento { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual ICollection<Inscricaopessoaevento> Inscricaopessoaeventos { get; set; } = new List<Inscricaopessoaevento>();
}
