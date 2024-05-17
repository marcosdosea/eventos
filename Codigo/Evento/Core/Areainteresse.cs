using System;
using System.Collections.Generic;

namespace Core;

public partial class Areainteresse
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Pessoaareainteresse> Pessoaareainteresses { get; set; } = new List<Pessoaareainteresse>();

    public virtual ICollection<Evento> IdEventos { get; set; } = new List<Evento>();
}
