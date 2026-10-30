using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EventoWeb.Helpers
{
    /// <summary>
    /// Utilitários compartilhados para geração de arquivos CSV.
    /// </summary>
    public static class CsvFormat
    {
        /// <summary>
        /// Escapa um campo de texto e neutraliza fórmulas de planilha (CSV Injection).
        /// Trata também fórmulas precedidas por espaços em branco no início.
        /// </summary>
        public static string Campo(string? valor)
        {
            valor ??= string.Empty;
            var inicio = valor.TrimStart();
            if (inicio.Length > 0 && (inicio[0] == '=' || inicio[0] == '+' || inicio[0] == '-' || inicio[0] == '@' || inicio[0] == '\t' || inicio[0] == '\r'))
            {
                valor = "'" + valor;
            }

            if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }

            return valor;
        }

        /// <summary>
        /// Converte um texto em slug seguro para nome de arquivo.
        /// </summary>
        public static string Slug(string? nome, string fallback = "evento")
        {
            if (string.IsNullOrWhiteSpace(nome))
                return fallback;

            var normalizado = nome.Normalize(NormalizationForm.FormD);
            var semAcentos = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    semAcentos.Append(c);
            }

            var slug = Regex
                .Replace(semAcentos.ToString(), "[^a-zA-Z0-9]+", "-")
                .Trim('-')
                .ToLowerInvariant();

            if (slug.Length > 60)
                slug = slug.Substring(0, 60).Trim('-');

            return string.IsNullOrEmpty(slug) ? fallback : slug;
        }

        /// <summary>
        /// Prefixa o conteúdo com o BOM UTF-8 para que o Excel reconheça acentos,
        /// com alocação otimizada sem iteradores LINQ.
        /// </summary>
        public static byte[] ComBom(string conteudo)
        {
            conteudo ??= string.Empty;
            var preamble = Encoding.UTF8.GetPreamble();
            var bytes = Encoding.UTF8.GetBytes(conteudo);
            var resultado = new byte[preamble.Length + bytes.Length];
            Buffer.BlockCopy(preamble, 0, resultado, 0, preamble.Length);
            Buffer.BlockCopy(bytes, 0, resultado, preamble.Length, bytes.Length);
            return resultado;
        }
    }
}
