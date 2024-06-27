using System;
using System.Collections.Generic;

namespace Core;

public partial class Subevento
{
    public uint Id { get; set; }

    public uint IdEvento { get; set; }

    public string Nome { get; set; } = null!;

    public string Descricao { get; set; } = null!;

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }

    public sbyte InscricaoGratuita { get; set; }

    /// <summary>
    /// C- CADASTRO\nA- ABERTO\nF- FINALIZADO\n 
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime DataInicioInscricao { get; set; }

    public DateTime DataFimInscricao { get; set; }

    public decimal ValorInscricaoMaisBarata { get; set; }

    public sbyte PossuiCertificado { get; set; }

    public decimal FrequenciaMinimaCertificado { get; set; }

    public int VagasOfertadas { get; set; }

    public int VagasReservadas { get; set; }

    public int VagasDisponiveis { get; set; }

    public int CargaHoraria { get; set; }

    public uint IdTipoEvento { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Tipoevento IdTipoEventoNavigation { get; set; } = null!;
}
