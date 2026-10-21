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

        public static byte[] Gerar(DashboardCsvContext contexto, DashboardFinanceiroDTO fin, DashboardOperacaoDTO op)
        {
            ArgumentNullException.ThrowIfNull(contexto);
            ArgumentNullException.ThrowIfNull(fin);
            ArgumentNullException.ThrowIfNull(op);

            var sb = new StringBuilder();
            sb.AppendLine(Cabecalho);

            // Metadados e filtros aplicados
            LinhaTexto(sb, "meta", "evento", "id", contexto.IdEvento.ToString(CultureInfo.InvariantCulture));
            LinhaTexto(sb, "meta", "evento", "nome", contexto.NomeEvento);
            LinhaTexto(sb, "meta", "evento", "status", contexto.Status);
            LinhaTexto(sb, "meta", "exportacao", "gerado_em", contexto.GeradoEm.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            LinhaTexto(sb, "meta", "filtros", "subevento_id", contexto.IdSubevento?.ToString(CultureInfo.InvariantCulture) ?? "todos");
            LinhaTexto(sb, "meta", "filtros", "tipo_inscricao_id", contexto.IdTipoInscricao?.ToString(CultureInfo.InvariantCulture) ?? "todos");
            LinhaTexto(sb, "meta", "filtros", "periodo_inicio", contexto.DataInicio?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
            LinhaTexto(sb, "meta", "filtros", "periodo_fim", contexto.DataFim?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);

            // Finanças - resumo
            LinhaNumero(sb, "financas", "resumo", "total_inscritos", quantidade: IntCsv(fin.TotalInscritos));
            LinhaNumero(sb, "financas", "resumo", "vagas_ofertadas", quantidade: IntCsv(fin.VagasOfertadas));
            LinhaNumero(sb, "financas", "resumo", "percentual_ocupacao", percentual: PctCsv(fin.PercentualOcupacao));
            LinhaNumero(sb, "financas", "resumo", "receita_confirmada", valor: NumCsv(fin.ReceitaConfirmada));
            LinhaNumero(sb, "financas", "resumo", "valor_em_aberto", valor: NumCsv(fin.ValorEmAberto));
            LinhaNumero(sb, "financas", "resumo", "ticket_medio", valor: NumCsv(fin.TicketMedio));
            LinhaNumero(sb, "financas", "resumo", "pagamentos_em_aberto", quantidade: IntCsv(fin.PagamentosEmAberto));

            // Finanças - funil
            LinhaNumero(sb, "financas", "funil", "ativas", quantidade: IntCsv(fin.Funil.Ativas), percentual: PctCsv(fin.Funil.AtivasPercentual));
            LinhaNumero(sb, "financas", "funil", "solicitadas", quantidade: IntCsv(fin.Funil.Solicitadas), percentual: PctCsv(fin.Funil.SolicitadasPercentual));
            LinhaNumero(sb, "financas", "funil", "canceladas", quantidade: IntCsv(fin.Funil.Canceladas), percentual: PctCsv(fin.Funil.CanceladasPercentual));

            // Finanças - formas de pagamento
            foreach (var forma in fin.PorForma)
                LinhaNumero(sb, "financas", "forma", forma.NomeExibicao, quantidade: IntCsv(forma.Quantidade), valor: NumCsv(forma.ValorTotal), percentual: PctCsv(forma.Percentual));

            // Finanças - categorias
            foreach (var categoria in fin.PorCategoria)
                LinhaNumero(sb, "financas", "categoria", categoria.Nome, quantidade: IntCsv(categoria.Quantidade), valor: NumCsv(categoria.ValorTotal), percentual: PctCsv(categoria.Percentual));

            // Operação - resumo
            LinhaNumero(sb, "operacao", "resumo", "presentes", quantidade: IntCsv(op.Presentes));
            LinhaNumero(sb, "operacao", "resumo", "ausentes", quantidade: IntCsv(op.Ausentes));
            LinhaNumero(sb, "operacao", "resumo", "total", quantidade: IntCsv(op.Total));
            LinhaNumero(sb, "operacao", "resumo", "aptos_certificado", quantidade: IntCsv(op.AptosCertificado));
            LinhaNumero(sb, "operacao", "resumo", "frequencia_media", percentual: PctCsv(op.FrequenciaMedia));

            // Operação - ocupação por subevento
            foreach (var sub in op.OcupacaoSubeventos)
                LinhaNumero(sb, "operacao", "subevento", sub.Nome, quantidade: IntCsv(sub.Ocupados), vagas: IntCsv(sub.VagasOfertadas), percentual: PctCsv(sub.Percentual));

            // Operação - distribuição por sexo
            LinhaNumero(sb, "operacao", "sexo", "feminino", quantidade: IntCsv(op.PorSexo.Feminino), percentual: PctCsv(op.PorSexo.FemininoPercentual));
            LinhaNumero(sb, "operacao", "sexo", "masculino", quantidade: IntCsv(op.PorSexo.Masculino), percentual: PctCsv(op.PorSexo.MasculinoPercentual));
            var totalSexo = op.PorSexo.Feminino + op.PorSexo.Masculino + op.PorSexo.NaoInformado;
            var naoInformadoPct = totalSexo > 0 ? Math.Round((decimal)op.PorSexo.NaoInformado * 100 / totalSexo, 2) : 0;
            LinhaNumero(sb, "operacao", "sexo", "nao_informado", quantidade: IntCsv(op.PorSexo.NaoInformado), percentual: PctCsv(naoInformadoPct));

            // Operação - origem geográfica
            foreach (var origem in op.PorOrigem)
                LinhaNumero(sb, "operacao", "origem", origem.Estado, quantidade: IntCsv(origem.Quantidade), percentual: PctCsv(origem.Percentual));

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public static string NomeArquivo(string? nomeEvento, uint idEvento, DateTime geradoEm)
        {
            return $"dashboard-{Slug(nomeEvento)}-{idEvento}-{geradoEm.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.csv";
        }

        /// <summary>
        /// Escapa um campo de texto e neutraliza fórmulas de planilha (CSV Injection).
        /// </summary>
        public static string CampoCsv(string? valor)
        {
            valor ??= string.Empty;
            if (valor.Length > 0 && (valor[0] == '=' || valor[0] == '+' || valor[0] == '-' || valor[0] == '@' || valor[0] == '\t' || valor[0] == '\r'))
            {
                valor = "'" + valor;
            }

            if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }

            return valor;
        }

        public static string NumCsv(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);

        public static string PctCsv(decimal valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

        public static string IntCsv(int valor) => valor.ToString(CultureInfo.InvariantCulture);

        private static void LinhaNumero(StringBuilder sb, string secao, string grupo, string item,
            string quantidade = "", string vagas = "", string valor = "", string percentual = "")
        {
            sb.Append(CampoCsv(secao)).Append(';')
              .Append(CampoCsv(grupo)).Append(';')
              .Append(CampoCsv(item)).Append(';')
              .Append(quantidade).Append(';')
              .Append(vagas).Append(';')
              .Append(valor).Append(';')
              .Append(percentual)
              .AppendLine();
        }

        private static void LinhaTexto(StringBuilder sb, string secao, string grupo, string item, string? valor)
        {
            sb.Append(CampoCsv(secao)).Append(';')
              .Append(CampoCsv(grupo)).Append(';')
              .Append(CampoCsv(item)).Append(';')
              .Append(';')
              .Append(';')
              .Append(CampoCsv(valor)).Append(';')
              .AppendLine();
        }

        private static string Slug(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return "evento";

            var normalizado = nome.Normalize(NormalizationForm.FormD);
            var semAcentos = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    semAcentos.Append(c);
            }

            var slug = System.Text.RegularExpressions.Regex
                .Replace(semAcentos.ToString(), "[^a-zA-Z0-9]+", "-")
                .Trim('-')
                .ToLowerInvariant();

            if (slug.Length > 60)
                slug = slug.Substring(0, 60).Trim('-');

            return string.IsNullOrEmpty(slug) ? "evento" : slug;
        }
    }
}
