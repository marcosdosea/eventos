using System.Globalization;
using System.Text;

namespace EventoWeb.Helpers
{
    /// <summary>
    /// Linha de frequência já desacoplada das entidades, usada na exportação CSV (Issue #880).
    /// </summary>
    public sealed class FrequenciaCsvItem
    {
        public string? Nome { get; set; }
        public string? Cpf { get; set; }
        public string? Email { get; set; }
        public DateTime Entrada { get; set; }
        public DateTime? Saida { get; set; }
    }

    /// <summary>
    /// Gera o CSV da tela de frequência por evento/subevento (Issue #880).
    /// </summary>
    public static class FrequenciaCsvExporter
    {
        public const string Cabecalho = "evento;subevento;nome;cpf;email;entrada;saida;permanencia_minutos";

        /// <summary>
        /// Gera as linhas do CSV (cabeçalho + dados) para pré-visualização e serialização.
        /// </summary>
        public static List<string[]> GerarLinhas(string? nomeEvento, string? nomeSubEvento, IEnumerable<FrequenciaCsvItem> itens)
        {
            ArgumentNullException.ThrowIfNull(itens);

            var linhas = new List<string[]> { Cabecalho.Split(';') };
            var evento = CsvFormat.Campo(nomeEvento);
            var subEvento = CsvFormat.Campo(nomeSubEvento);

            foreach (var item in itens)
            {
                int? permanencia = item.Saida.HasValue
                    ? Math.Max(0, (int)Math.Round((item.Saida.Value - item.Entrada).TotalMinutes, MidpointRounding.AwayFromZero))
                    : null;

                linhas.Add(new[]
                {
                    evento,
                    subEvento,
                    CsvFormat.Campo(item.Nome),
                    CsvFormat.Campo(item.Cpf),
                    CsvFormat.Campo(item.Email),
                    item.Entrada.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                    item.Saida?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) ?? string.Empty,
                    permanencia?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
                });
            }

            return linhas;
        }

        public static byte[] Gerar(string? nomeEvento, string? nomeSubEvento, IEnumerable<FrequenciaCsvItem> itens)
        {
            var sb = new StringBuilder();
            foreach (var linha in GerarLinhas(nomeEvento, nomeSubEvento, itens))
            {
                sb.AppendJoin(';', linha).AppendLine();
            }

            return CsvFormat.ComBom(sb.ToString());
        }

        public static string NomeArquivo(
            string? nomeEvento,
            uint idEvento,
            string? nomeSubEvento,
            uint? idSubEvento,
            DateTime geradoEm)
        {
            var partes = new List<string>
            {
                "frequencia",
                CsvFormat.Slug(nomeEvento, "evento"),
                idEvento.ToString(CultureInfo.InvariantCulture)
            };

            if (idSubEvento.HasValue && idSubEvento.Value > 0)
            {
                partes.Add(CsvFormat.Slug(nomeSubEvento, "subevento"));
                partes.Add(idSubEvento.Value.ToString(CultureInfo.InvariantCulture));
            }

            partes.Add(geradoEm.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture));
            return string.Join("-", partes) + ".csv";
        }
    }
}
