using System;
using System.Collections.Generic;

namespace Core;

public partial class Modelocracha
{
    public int Id { get; set; }

    public byte[] Logotipo { get; set; } = null!;

    public string Texto { get; set; } = null!;

    public sbyte Qrcode { get; set; }

    public uint IdEvento { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;
}
