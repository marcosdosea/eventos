namespace Core.DTO
{
    public class FormaPagamentoDTO
    {
        /// <summary>D/C/B/P - Dinheiro/Cartão/Boleto/Pix (ver Pagamento.Forma).</summary>
        public string Forma { get; set; } = null!;
        public string NomeExibicao { get; set; } = null!;
        public int Quantidade { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal Percentual { get; set; }
    }

    public class CategoriaInscricaoDTO
    {
        public uint? IdTipoInscricao { get; set; }
        public string Nome { get; set; } = null!;
        public int Quantidade { get; set; }
        public decimal Percentual { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class FunilInscricoesDTO
    {
        // Status Inscricaopessoaevento: A=Ativa, S=Solicitada, C=Cancelada
        public int Ativas { get; set; }
        public decimal AtivasPercentual { get; set; }
        public int Solicitadas { get; set; }
        public decimal SolicitadasPercentual { get; set; }
        public int Canceladas { get; set; }
        public decimal CanceladasPercentual { get; set; }
    }

    /// <summary>
    /// Aba 1 do protótipo: Visão Geral &amp; Finanças.
    /// </summary>
    public class DashboardFinanceiroDTO
    {
        public int TotalInscritos { get; set; }
        public int VagasOfertadas { get; set; }
        public decimal PercentualOcupacao { get; set; }

        public decimal ReceitaConfirmada { get; set; }
        public decimal ValorEmAberto { get; set; }
        public decimal TicketMedio { get; set; }
        public int PagamentosEmAberto { get; set; }

        public List<FormaPagamentoDTO> PorForma { get; set; } = new();
        public List<CategoriaInscricaoDTO> PorCategoria { get; set; } = new();
        public FunilInscricoesDTO Funil { get; set; } = new();
    }
}
