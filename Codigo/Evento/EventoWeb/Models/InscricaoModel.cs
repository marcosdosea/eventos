using Core;
using System.ComponentModel.DataAnnotations;
namespace EventoWeb.Models;

public class InscricaoModel
  {
    public uint Id { get; set; }

    public uint IdPessoa { get; set; }

    public uint IdEvento { get; set; }

    public int IdPapel { get; set; }

    public uint? IdTipoInscricao { get; set; }

    public DateTime DataInscricao { get; set; }

    public decimal ValorTotal { get; set; }

    /// <summary>
    /// A - ATIVA
    /// C - CANCELADA
    /// S - SOLICITADA
    /// 
    /// </summary>
    public string Status { get; set; } = null!;

    public decimal FrequenciaFinal { get; set; }

    public string? NomeCracha { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Papel IdPapelNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;

    public virtual Tipoinscricao? IdTipoInscricaoNavigation { get; set; }

    public virtual ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
}
