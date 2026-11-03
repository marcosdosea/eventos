using Core.DTO;
using System.Globalization;
using System.Text;

namespace EventoWeb.Helpers
{
    /// <summary>
    /// Contexto do evento e dos filtros usados na geração do CSV do Dashboard (Issue #856).
    /// </summary>
    public class DashboardCsvContext
    {
        public uint IdEvento { get; set; }
        public string NomeEvento { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public uint? IdSubevento { get; set; }
        public uint? IdTipoInscricao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public DateTime GeradoEm { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Gera o CSV do Dashboard em formato "long" com 7 colunas fixas:
    /// secao;grupo;item;quantidade;vagas;valor;percentual.
    /// </summary>
    public static class DashboardCsvExporter
    {
        public const string Cabecalho = "secao;grupo;item;quantidade;vagas;valor;percentual";

        /// <summary>
        /// Gera as linhas do CSV (cabeçalho + dados) para pré-visualização e serialização.
        /// </summary>
        public static List<string[]> GerarLinhas(DashboardCsvContext contexto, DashboardFinanceiroDTO fin, DashboardOperacaoDTO op)
        {
            ArgumentNullException.ThrowIfNull(contexto);
            ArgumentNullException.ThrowIfNull(fin);
            ArgumentNullException.ThrowIfNull(op);

            var linhas = new List<string[]> { Cabecalho.Split(';') };

            void Adicionar(string secao, string grupo, string item,
                string quantidade = "", string vagas = "", string valor = "", string percentual = "")
            {
                linhas.Add(new[] { secao, grupo, item, quantidade, vagas, valor, percentual });
            }

            // Metadados e filtros aplicados
            Adicionar("meta", "evento", "id", valor: contexto.IdEvento.ToString(CultureInfo.InvariantCulture));
            Adicionar("meta", "evento", "nome", valor: contexto.NomeEvento);
            Adicionar("meta", "evento", "status", valor: contexto.Status);
            Adicionar("meta", "exportacao", "gerado_em", valor: contexto.GeradoEm.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            Adicionar("meta", "filtros", "subevento_id", valor: contexto.IdSubevento?.ToString(CultureInfo.InvariantCulture) ?? "todos");
            Adicionar("meta", "filtros", "tipo_inscricao_id", valor: contexto.IdTipoInscricao?.ToString(CultureInfo.InvariantCulture) ?? "todos");
            Adicionar("meta", "filtros", "periodo_inicio", valor: contexto.DataInicio?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
            Adicionar("meta", "filtros", "periodo_fim", valor: contexto.DataFim?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);

            // Finanças - resumo
            Adicionar("financas", "resumo", "total_inscritos", quantidade: IntCsv(fin.TotalInscritos));
            Adicionar("financas", "resumo", "vagas_ofertadas", quantidade: IntCsv(fin.VagasOfertadas));
            Adicionar("financas", "resumo", "percentual_ocupacao", percentual: PctCsv(fin.PercentualOcupacao));
            Adicionar("financas", "resumo", "receita_confirmada", valor: NumCsv(fin.ReceitaConfirmada));
            Adicionar("financas", "resumo", "valor_em_aberto", valor: NumCsv(fin.ValorEmAberto));
            Adicionar("financas", "resumo", "ticket_medio", valor: NumCsv(fin.TicketMedio));
            Adicionar("financas", "resumo", "pagamentos_em_aberto", quantidade: IntCsv(fin.PagamentosEmAberto));

            // Finanças - funil
            Adicionar("financas", "funil", "ativas", quantidade: IntCsv(fin.Funil.Ativas), percentual: PctCsv(fin.Funil.AtivasPercentual));
            Adicionar("financas", "funil", "solicitadas", quantidade: IntCsv(fin.Funil.Solicitadas), percentual: PctCsv(fin.Funil.SolicitadasPercentual));
            Adicionar("financas", "funil", "canceladas", quantidade: IntCsv(fin.Funil.Canceladas), percentual: PctCsv(fin.Funil.CanceladasPercentual));

            // Finanças - formas de pagamento
            foreach (var forma in fin.PorForma)
                Adicionar("financas", "forma", forma.NomeExibicao, quantidade: IntCsv(forma.Quantidade), valor: NumCsv(forma.ValorTotal), percentual: PctCsv(forma.Percentual));

            // Finanças - categorias
            foreach (var categoria in fin.PorCategoria)
                Adicionar("financas", "categoria", categoria.Nome, quantidade: IntCsv(categoria.Quantidade), valor: NumCsv(categoria.ValorTotal), percentual: PctCsv(categoria.Percentual));

            // Operação - resumo
            Adicionar("operacao", "resumo", "presentes", quantidade: IntCsv(op.Presentes));
            Adicionar("operacao", "resumo", "ausentes", quantidade: IntCsv(op.Ausentes));
            Adicionar("operacao", "resumo", "total", quantidade: IntCsv(op.Total));
            Adicionar("operacao", "resumo", "aptos_certificado", quantidade: IntCsv(op.AptosCertificado));
            Adicionar("operacao", "resumo", "frequencia_media", percentual: PctCsv(op.FrequenciaMedia));

            // Operação - ocupação por subevento
            foreach (var sub in op.OcupacaoSubeventos)
                Adicionar("operacao", "subevento", sub.Nome, quantidade: IntCsv(sub.Ocupados), vagas: IntCsv(sub.VagasOfertadas), percentual: PctCsv(sub.Percentual));

            // Operação - distribuição por sexo
            Adicionar("operacao", "sexo", "feminino", quantidade: IntCsv(op.PorSexo.Feminino), percentual: PctCsv(op.PorSexo.FemininoPercentual));
            Adicionar("operacao", "sexo", "masculino", quantidade: IntCsv(op.PorSexo.Masculino), percentual: PctCsv(op.PorSexo.MasculinoPercentual));
            var totalSexo = op.PorSexo.Feminino + op.PorSexo.Masculino + op.PorSexo.NaoInformado;
            var naoInformadoPct = totalSexo > 0 ? Math.Round((decimal)op.PorSexo.NaoInformado * 100 / totalSexo, 2) : 0;
            Adicionar("operacao", "sexo", "nao_informado", quantidade: IntCsv(op.PorSexo.NaoInformado), percentual: PctCsv(naoInformadoPct));

            // Operação - origem geográfica
            foreach (var origem in op.PorOrigem)
                Adicionar("operacao", "origem", origem.Estado, quantidade: IntCsv(origem.Quantidade), percentual: PctCsv(origem.Percentual));

            return linhas;
        }

        public static byte[] Gerar(DashboardCsvContext contexto, DashboardFinanceiroDTO fin, DashboardOperacaoDTO op)
        {
            var sb = new StringBuilder();
            foreach (var linha in GerarLinhas(contexto, fin, op))
            {
                var valor = linha[0] == "meta" ? CampoCsv(linha[5]) : linha[5];
                sb.Append(CampoCsv(linha[0])).Append(';')
                  .Append(CampoCsv(linha[1])).Append(';')
                  .Append(CampoCsv(linha[2])).Append(';')
                  .Append(linha[3]).Append(';')
                  .Append(linha[4]).Append(';')
                  .Append(valor).Append(';')
                  .Append(linha[6])
                  .AppendLine();
            }

            return CsvFormat.ComBom(sb.ToString());
        }

        public static string NomeArquivo(string? nomeEvento, uint idEvento, DateTime geradoEm)
        {
            return $"dashboard-{Slug(nomeEvento)}-{idEvento}-{geradoEm.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.csv";
        }

        /// <summary>
        /// Escapa um campo de texto e neutraliza fórmulas de planilha (CSV Injection).
        /// </summary>
        public static string CampoCsv(string? valor) => CsvFormat.Campo(valor);

        public static string NumCsv(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);

        public static string PctCsv(decimal valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

        public static string IntCsv(int valor) => valor.ToString(CultureInfo.InvariantCulture);

        private static string Slug(string? nome) => CsvFormat.Slug(nome);
    }
}
