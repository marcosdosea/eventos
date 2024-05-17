using System;
using System.Collections.Generic;

namespace Core;

public partial class Participacaopessoasubevento
{
    public uint Id { get; set; }

    public uint IdPessoa { get; set; }

    public uint IdSubEvento { get; set; }

    public DateTime Entrada { get; set; }

    public DateTime? Saida { get; set; }

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;

    public virtual Subevento IdSubEventoNavigation { get; set; } = null!;
}
