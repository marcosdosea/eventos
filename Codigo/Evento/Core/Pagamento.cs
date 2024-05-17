using System;
using System.Collections.Generic;

namespace Core;

public partial class Pagamento
{
    public uint Id { get; set; }

    public uint IdPessoaInscricaoPessoaEvento { get; set; }

    public uint IdEventoInscricaoPessoaEvento { get; set; }

    public decimal Valor { get; set; }

    /// <summary>
    /// S- SOLICITADO
    /// C- CONFIRMADO
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// D - DINHEIRO
    /// C- CARTAO
    /// B - BOLETO
    /// P - PIX
    /// </summary>
    public string Forma { get; set; } = null!;

    public string? CodigoPagamento { get; set; }

    public virtual Inscricaopessoaevento IdNavigation { get; set; } = null!;
}
