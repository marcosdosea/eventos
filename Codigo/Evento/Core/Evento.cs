using System;
using System.Collections.Generic;

namespace Core;

public partial class Evento
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Descricao { get; set; } = null!;

    public DateTime DataInicio { get; set; }

    public DateTime DataFim { get; set; }

    public sbyte InscricaoGratuita { get; set; }

    /// <summary>
    /// C- CADASTRO
    /// A- ATIVO
    /// I - INATIVO
    /// F- FINALIZADO
    ///  
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime DataInicioInscricao { get; set; }

    public DateTime DataFimInscricao { get; set; }

    public decimal ValorInscricao { get; set; }

    public string? Website { get; set; }

    public string? EmailEvento { get; set; }

    public sbyte EventoPublico { get; set; }

    public string Cep { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string Cidade { get; set; } = null!;

    public string Bairro { get; set; } = null!;

    public string Rua { get; set; } = null!;

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public sbyte PossuiCertificado { get; set; }

    public decimal FrequenciaMinimaCertificado { get; set; }

    public int IdTipoEvento { get; set; }

    public int VagasOfertadas { get; set; }

    public int VagasReservadas { get; set; }

    public int VagasDisponiveis { get; set; }

    public int TempoMinutosReserva { get; set; }

    public int CargaHoraria { get; set; }

    public virtual Estadosbrasil EstadoNavigation { get; set; } = null!;

    public virtual Tipoevento IdTipoEventoNavigation { get; set; } = null!;

    public virtual ICollection<Inscricaopessoaevento> Inscricaopessoaeventos { get; set; } = new List<Inscricaopessoaevento>();

    public virtual ICollection<Modelocertificado> Modelocertificados { get; set; } = new List<Modelocertificado>();

    public virtual ICollection<Modelocracha> Modelocrachas { get; set; } = new List<Modelocracha>();

    public virtual ICollection<Participacaopessoaevento> Participacaopessoaeventos { get; set; } = new List<Participacaopessoaevento>();

    public virtual ICollection<Subevento> Subeventos { get; set; } = new List<Subevento>();

    public virtual ICollection<Tipoinscricao> Tipoinscricaos { get; set; } = new List<Tipoinscricao>();

    public virtual ICollection<Areainteresse> IdAreaInteresses { get; set; } = new List<Areainteresse>();
}
