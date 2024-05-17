using System;
using System.Collections.Generic;

namespace Core;

public partial class Tipoinscricaoevento
{
    public int Id { get; set; }

    public uint IdEvento { get; set; }

    public string Descricao { get; set; } = null!;

    public decimal Valor { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime Datafim { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual ICollection<Inscricaopessoaevento> Inscricaopessoaeventos { get; set; } = new List<Inscricaopessoaevento>();
}
