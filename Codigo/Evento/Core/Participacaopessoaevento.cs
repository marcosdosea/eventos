using System;
using System.Collections.Generic;

namespace Core;

public partial class Participacaopessoaevento
{
    public uint Id { get; set; }

    public uint IdPessoa { get; set; }

    public uint IdEvento { get; set; }

    public DateTime Entrada { get; set; }

    public DateTime? Saida { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;
}
