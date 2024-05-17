using System;
using System.Collections.Generic;

namespace Core;

public partial class Estadosbrasil
{
    public string Estado { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public virtual ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
