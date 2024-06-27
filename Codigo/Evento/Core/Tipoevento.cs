using System;
using System.Collections.Generic;

namespace Core;

public partial class Tipoevento
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();

    public virtual ICollection<Subevento> Subeventos { get; set; } = new List<Subevento>();
}
