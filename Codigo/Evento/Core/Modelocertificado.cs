using System;
using System.Collections.Generic;

namespace Core;

public partial class Modelocertificado
{
    public uint Id { get; set; }

    public byte[] LogotipoSuperior { get; set; } = null!;

    public string TextoAntesParticipante { get; set; } = null!;

    public string TextoAntesEvento { get; set; } = null!;

    public string TextoAntesCargaHoraria { get; set; } = null!;

    public string Assinatura1Texto { get; set; } = null!;

    public byte[] Assinatura1 { get; set; } = null!;

    public string? Assinatura2Texto { get; set; }

    public byte[]? Assinatura2 { get; set; }

    public uint IdEvento { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;
}
