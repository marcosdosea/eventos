using System;
using System.Collections.Generic;

namespace Core;

public partial class Tipoinscricaosubevento
{
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;

    public decimal Valor { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime Datafim { get; set; }

    public uint IdSubEvento { get; set; }

    public virtual Subevento IdSubEventoNavigation { get; set; } = null!;

    public virtual ICollection<Inscricaopessoasubevento> Inscricaopessoasubeventos { get; set; } = new List<Inscricaopessoasubevento>();
}
