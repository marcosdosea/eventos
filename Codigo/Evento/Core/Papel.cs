using System;
using System.Collections.Generic;

namespace Core;

public partial class Papel
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Inscricaopessoaevento> Inscricaopessoaeventos { get; set; } = new List<Inscricaopessoaevento>();
}
