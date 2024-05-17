using System;
using System.Collections.Generic;

namespace Core;

public partial class Areainteresse
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Evento> IdEventos { get; set; } = new List<Evento>();

    public virtual ICollection<Pessoa> IdPessoas { get; set; } = new List<Pessoa>();
}
