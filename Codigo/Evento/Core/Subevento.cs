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
    /// C- CADASTRO
    /// A- ABERTO
    /// F- FINALIZADO
    ///  
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime DataInicioInscricao { get; set; }

    public DateTime DataFimInscricao { get; set; }

    public decimal ValorInscricao { get; set; }

    public sbyte PossuiCertificado { get; set; }

    public decimal FrequenciaMinimaCertificado { get; set; }

    public uint VagasOfertadas { get; set; }

    public uint VagasReservadas { get; set; }

    public uint VagasDisponiveis { get; set; }

    public uint CargaHoraria { get; set; }

    public uint IdTipoEvento { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Tipoevento IdTipoEventoNavigation { get; set; } = null!;

    public virtual ICollection<Inscricaopessoasubevento> Inscricaopessoasubeventos { get; set; } = new List<Inscricaopessoasubevento>();

    public virtual ICollection<Participacaopessoasubevento> Participacaopessoasubeventos { get; set; } = new List<Participacaopessoasubevento>();

    public virtual ICollection<Tipoinscricao> IdTipoInscricaos { get; set; } = new List<Tipoinscricao>();
}
