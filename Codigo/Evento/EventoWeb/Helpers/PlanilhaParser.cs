using System.Globalization;
using System.Text;
using Core.DTO;
using CsvHelper;
using CsvHelper.Configuration;
using EventoWeb.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Util;

namespace EventoWeb.Helpers
{
    /// <summary>
    /// Interface para serviços de leitura, extração e validação de planilhas de inscritos do SIGAA.
    /// </summary>
    public interface IPlanilhaParser
    {
        ResultadoParsingSigaaDto Parse(Stream stream, string? fileNameOuExtensao = null);
        ResultadoParsingSigaaDto Parse(byte[] fileBytes, string? fileNameOuExtensao = null);
        ResultadoParsingSigaaDto Parse(IFormFile file);
        ResultadoParsingSigaaDto ParseCsv(Stream stream, Encoding? encoding = null);
        ResultadoParsingSigaaDto ParseCsv(string conteudo);
        ResultadoParsingSigaaDto ParseExcel(Stream stream, string? extensao = null);
    }

    /// <summary>
    /// Parser especializado na leitura, extração e validação sintática de arquivos de planilha
    /// exportados do SIGAA nos formatos CSV (.csv) e Excel (.xlsx, .xls).
    /// </summary>
    public class PlanilhaParser : IPlanilhaParser
    {
        static PlanilhaParser()
        {
            // Registra os provedores de codificação necessários para páginas legadas (como ISO-8859-1 e BIFF do Excel)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #region Métodos Públicos de Entrada

        public ResultadoParsingSigaaDto Parse(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                var resultadoVazio = new ResultadoParsingSigaaDto();
                resultadoVazio.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = "Arquivo vazio ou não fornecido."
                });
                return resultadoVazio;
            }

            using var stream = file.OpenReadStream();
            return Parse(stream, file.FileName);
        }

        public ResultadoParsingSigaaDto Parse(byte[] fileBytes, string? fileNameOuExtensao = null)
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                var resultadoVazio = new ResultadoParsingSigaaDto();
                resultadoVazio.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = "Arquivo vazio ou não fornecido."
                });
                return resultadoVazio;
            }

            using var ms = new MemoryStream(fileBytes);
            return Parse(ms, fileNameOuExtensao);
        }

        public ResultadoParsingSigaaDto Parse(Stream stream, string? fileNameOuExtensao = null)
        {
            if (stream == null)
            {
                var resultadoVazio = new ResultadoParsingSigaaDto();
                resultadoVazio.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = "Arquivo vazio ou não fornecido."
                });
                return resultadoVazio;
            }

            try
            {
                using var memoryStream = new MemoryStream();
                if (stream.CanSeek)
                    stream.Position = 0;
                stream.CopyTo(memoryStream);

                if (memoryStream.Length == 0)
                {
                    var resultadoVazio = new ResultadoParsingSigaaDto();
                    resultadoVazio.ListaErros.Add(new ErroParsingSigaaDto
                    {
                        Linha = 0,
                        ValorBruto = null,
                        MotivoRecusa = "Arquivo vazio ou sem dados."
                    });
                    return resultadoVazio;
                }

                memoryStream.Position = 0;
                var ext = (Path.GetExtension(fileNameOuExtensao) ?? fileNameOuExtensao ?? string.Empty).ToLowerInvariant().Trim();

                if (ext == ".xlsx" || ext == "xlsx")
                {
                    return ParseExcel(memoryStream, ".xlsx");
                }
                if (ext == ".xls" || ext == "xls")
                {
                    return ParseExcel(memoryStream, ".xls");
                }
                if (ext == ".csv" || ext == "csv")
                {
                    return ParseCsv(memoryStream);
                }

                // Identificação por assinatura de bytes (Magic Bytes)
                var bytes = memoryStream.ToArray();
                memoryStream.Position = 0;

                // Magic bytes do ZIP / XLSX (PK..)
                if (bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
                {
                    return ParseExcel(memoryStream, ".xlsx");
                }

                // Magic bytes do arquivo composto OLE2 / XLS (0xD0, 0xCF, 0x11, 0xE0)
                if (bytes.Length >= 8 && bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0)
                {
                    return ParseExcel(memoryStream, ".xls");
                }

                // Padrão: CSV / texto delimitado
                return ParseCsv(memoryStream);
            }
            catch (Exception ex)
            {
                var erro = new ResultadoParsingSigaaDto();
                erro.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = $"Erro inesperado ao processar arquivo: {ex.Message}"
                });
                return erro;
            }
        }

        public ResultadoParsingSigaaDto ParseCsv(Stream stream, Encoding? encoding = null)
        {
            if (stream == null)
            {
                var res = new ResultadoParsingSigaaDto();
                res.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = "Arquivo vazio ou sem dados."
                });
                return res;
            }

            try
            {
                using var ms = new MemoryStream();
                if (stream.CanSeek)
                    stream.Position = 0;
                stream.CopyTo(ms);
                var bytes = ms.ToArray();

                if (bytes.Length == 0)
                {
                    var res = new ResultadoParsingSigaaDto();
                    res.ListaErros.Add(new ErroParsingSigaaDto
                    {
                        Linha = 0,
                        ValorBruto = null,
                        MotivoRecusa = "Arquivo vazio ou sem dados."
                    });
                    return res;
                }

                string texto;
                if (encoding != null)
                {
                    texto = encoding.GetString(bytes);
                }
                else
                {
                    var (_, detectedText) = DetectarEncodingETexto(bytes);
                    texto = detectedText;
                }

                return ParseCsv(texto);
            }
            catch (Exception ex)
            {
                var res = new ResultadoParsingSigaaDto();
                res.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = $"Erro ao ler arquivo CSV: {ex.Message}"
                });
                return res;
            }
        }

        public ResultadoParsingSigaaDto ParseCsv(string conteudo)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
            {
                var res = new ResultadoParsingSigaaDto();
                res.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = "Arquivo vazio ou sem dados."
                });
                return res;
            }

            try
            {
                var lines = conteudo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                var delimiter = DetectarDelimitadorCsv(lines);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = delimiter,
                    HasHeaderRecord = false,
                    BadDataFound = null,
                    MissingFieldFound = null,
                    TrimOptions = TrimOptions.Trim,
                    IgnoreBlankLines = false
                };

                var rawRows = new List<(int LineNumber, string[] Fields)>();
                using (var stringReader = new StringReader(conteudo))
                using (var csv = new CsvReader(stringReader, config))
                {
                    int lineNum = 0;
                    while (true)
                    {
                        try
                        {
                            if (!csv.Read())
                                break;
                        }
                        catch (Exception ex)
                        {
                            var res = new ResultadoParsingSigaaDto();
                            res.ListaErros.Add(new ErroParsingSigaaDto
                            {
                                Linha = lineNum + 1,
                                ValorBruto = null,
                                MotivoRecusa = $"Erro de formatação no arquivo CSV: {ex.Message}"
                            });
                            return res;
                        }

                        lineNum++;
                        int count = csv.Parser.Count;
                        var fields = new string[count];
                        for (int i = 0; i < count; i++)
                        {
                            fields[i] = csv.GetField(i)?.Trim() ?? string.Empty;
                        }
                        rawRows.Add((lineNum, fields));
                    }
                }

                return ProcessarLinhas(rawRows);
            }
            catch (Exception ex)
            {
                var res = new ResultadoParsingSigaaDto();
                res.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = $"Erro inesperado ao processar CSV: {ex.Message}"
                });
                return res;
            }
        }

        public ResultadoParsingSigaaDto ParseExcel(Stream stream, string? extensao = null)
        {
            if (stream == null || stream.Length == 0)
            {
                var res = new ResultadoParsingSigaaDto();
                res.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = "Arquivo vazio ou sem dados."
                });
                return res;
            }

            try
            {
                if (stream.CanSeek)
                    stream.Position = 0;

                using var reader = ExcelReaderFactory.CreateReader(stream);
                var rawRows = new List<(int LineNumber, string[] Fields)>();
                int lineNum = 0;

                while (reader.Read())
                {
                    lineNum++;
                    int count = reader.FieldCount;
                    var fields = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        var val = reader.GetValue(i);
                        fields[i] = ConverterValorCelula(val);
                    }
                    rawRows.Add((lineNum, fields));
                }

                return ProcessarLinhas(rawRows);
            }
            catch (Exception ex)
            {
                var res = new ResultadoParsingSigaaDto();
                res.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = $"Erro ao processar planilha Excel: {ex.Message}"
                });
                return res;
            }
        }

        #endregion

        #region Processamento Central e Validação

        /// <summary>
        /// Processa as linhas brutas extraídas da planilha, identificando metadados de cabeçalho,
        /// localizando o cabeçalho das colunas e validando cada linha de inscrição.
        /// </summary>
        private static ResultadoParsingSigaaDto ProcessarLinhas(List<(int LineNumber, string[] Fields)> rawRows)
        {
            var resultado = new ResultadoParsingSigaaDto();

            if (rawRows == null || rawRows.Count == 0)
            {
                resultado.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 0,
                    ValorBruto = null,
                    MotivoRecusa = "Arquivo vazio ou sem dados."
                });
                return resultado;
            }

            // 1. Localização do cabeçalho de colunas (ignorando linhas de título e metadados de emissão)
            int headerIndex = -1;
            int cpfCol = -1;
            int nomeCol = -1;
            int sobrenomeCol = -1;
            int emailCol = -1;
            int tipoCol = -1;

            for (int r = 0; r < rawRows.Count; r++)
            {
                var fields = rawRows[r].Fields;
                int potentialCpf = -1;
                int potentialNome = -1;
                int potentialSobrenome = -1;
                int potentialEmail = -1;
                int potentialTipo = -1;

                for (int c = 0; c < fields.Length; c++)
                {
                    var norm = NormalizarRotulo(fields[c]);
                    if (string.IsNullOrEmpty(norm))
                        continue;

                    if (potentialCpf == -1 && (norm == "cpf" || norm.Contains("cpf") || norm == "documento" || norm == "doc"))
                    {
                        potentialCpf = c;
                    }
                    else if (potentialSobrenome == -1 && (norm == "sobrenome" || norm.Contains("sobrenome")))
                    {
                        potentialSobrenome = c;
                    }
                    else if (potentialNome == -1 && !norm.Contains("sobrenome") && (norm == "nome" || norm == "nomecompleto" || norm == "nomedoinscrito" ||
                             norm == "nomedoparticipante" || (norm.Contains("nome") && !norm.Contains("evento")) ||
                             norm == "participante" || norm == "inscrito" || norm == "aluno"))
                    {
                        potentialNome = c;
                    }
                    else if (potentialEmail == -1 && (norm.Contains("email") || norm.Contains("correio")))
                    {
                        potentialEmail = c;
                    }
                    else if (potentialTipo == -1 && (norm.Contains("tipoparticipacao") || norm.Contains("tipoinscricao") ||
                             norm.Contains("tipodeparticipacao") || norm.Contains("tipo") || norm.Contains("categoria") ||
                             norm.Contains("modalidade") || norm.Contains("funcao")))
                    {
                        potentialTipo = c;
                    }
                }

                // Cabeçalho válido precisa ter ao menos CPF e Nome identificados
                if (potentialCpf != -1 && potentialNome != -1)
                {
                    headerIndex = r;
                    cpfCol = potentialCpf;
                    nomeCol = potentialNome;
                    sobrenomeCol = potentialSobrenome;
                    emailCol = potentialEmail;
                    tipoCol = potentialTipo;
                    break;
                }
            }

            if (headerIndex == -1)
            {
                resultado.ListaErros.Add(new ErroParsingSigaaDto
                {
                    Linha = 1,
                    ValorBruto = null,
                    MotivoRecusa = "Cabeçalho com colunas obrigatórias (CPF, Nome) não foi encontrado na planilha."
                });
                return resultado;
            }

            // 2. Processamento das linhas de dados a partir do cabeçalho
            for (int r = headerIndex + 1; r < rawRows.Count; r++)
            {
                var (lineNumber, fields) = rawRows[r];

                // Ignora linhas completamente vazias
                if (fields == null || fields.Length == 0 || fields.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                string rawCpf = cpfCol < fields.Length ? fields[cpfCol].Trim() : string.Empty;
                string rawNome = nomeCol < fields.Length ? fields[nomeCol].Trim() : string.Empty;
                string rawSobrenome = sobrenomeCol >= 0 && sobrenomeCol < fields.Length ? fields[sobrenomeCol].Trim() : string.Empty;
                string rawEmail = emailCol >= 0 && emailCol < fields.Length ? fields[emailCol].Trim() : string.Empty;
                string rawTipo = tipoCol >= 0 && tipoCol < fields.Length ? fields[tipoCol].Trim() : string.Empty;

                string nomeCompleto = string.IsNullOrEmpty(rawSobrenome)
                    ? rawNome
                    : $"{rawNome} {rawSobrenome}".Trim();

                bool linhaValida = true;

                // Validação de CPF
                if (string.IsNullOrWhiteSpace(rawCpf))
                {
                    resultado.ListaErros.Add(new ErroParsingSigaaDto
                    {
                        Linha = lineNumber,
                        ValorBruto = rawCpf,
                        MotivoRecusa = "CPF ausente"
                    });
                    linhaValida = false;
                }
                else if (!CpfValidator.Validar(rawCpf))
                {
                    resultado.ListaErros.Add(new ErroParsingSigaaDto
                    {
                        Linha = lineNumber,
                        ValorBruto = rawCpf,
                        MotivoRecusa = "CPF inválido"
                    });
                    linhaValida = false;
                }

                // Validação de Nome
                if (string.IsNullOrWhiteSpace(nomeCompleto))
                {
                    resultado.ListaErros.Add(new ErroParsingSigaaDto
                    {
                        Linha = lineNumber,
                        ValorBruto = rawNome,
                        MotivoRecusa = "Nome ausente"
                    });
                    linhaValida = false;
                }

                if (linhaValida)
                {
                    resultado.ListaSucesso.Add(new PessoaInscritaDto
                    {
                        Cpf = CpfValidator.Normalizar(rawCpf),
                        Nome = nomeCompleto,
                        Email = rawEmail,
                        TipoParticipacao = string.IsNullOrWhiteSpace(rawTipo) ? null : rawTipo
                    });
                }
            }

            return resultado;
        }

        #endregion

        #region Helpers de Normalização e Codificação

        public static (Encoding encoding, string text) DetectarEncodingETexto(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return (Encoding.UTF8, string.Empty);

            // UTF-8 com BOM (EF BB BF)
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return (Encoding.UTF8, Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
            }

            // Tenta decodificar como UTF-8 estrito
            try
            {
                var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                var text = utf8Strict.GetString(bytes);
                return (Encoding.UTF8, text);
            }
            catch (DecoderFallbackException)
            {
                // Em caso de falha de decodificação UTF-8, utiliza ISO-8859-1 (padrão comum do SIGAA)
                var iso = Encoding.GetEncoding("iso-8859-1");
                return (iso, iso.GetString(bytes));
            }
        }

        public static string DetectarDelimitadorCsv(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return ",";

            // Procura a linha com delimitadores nos primeiros registros
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int pontoEVirgula = line.Count(c => c == ';');
                int virgula = line.Count(c => c == ',');
                int tab = line.Count(c => c == '\t');

                if (pontoEVirgula == 0 && virgula == 0 && tab == 0)
                    continue;

                if (tab > pontoEVirgula && tab > virgula)
                    return "\t";

                if (pontoEVirgula >= virgula && pontoEVirgula > 0)
                    return ";";

                if (virgula > pontoEVirgula && virgula > 0)
                    return ",";
            }

            return ",";
        }

        private static string NormalizarRotulo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            var semAcento = Methods.RemoverAcentos(texto).ToLowerInvariant();
            var sb = new StringBuilder();
            foreach (var ch in semAcento)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string ConverterValorCelula(object? val)
        {
            if (val == null)
                return string.Empty;

            if (val is double d)
            {
                if (Math.Abs(d % 1) <= double.Epsilon)
                    return ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture);
                return d.ToString(CultureInfo.InvariantCulture);
            }
            if (val is float f)
            {
                if (Math.Abs(f % 1) <= float.Epsilon)
                    return ((long)Math.Round(f)).ToString(CultureInfo.InvariantCulture);
                return f.ToString(CultureInfo.InvariantCulture);
            }

            return val.ToString()?.Trim() ?? string.Empty;
        }

        #endregion

        #region Métodos Estáticos de Conveniência

        public static ResultadoParsingSigaaDto ParseFile(Stream stream, string? fileNameOuExtensao = null)
        {
            return new PlanilhaParser().Parse(stream, fileNameOuExtensao);
        }

        public static ResultadoParsingSigaaDto ParseCsvContent(string conteudo)
        {
            return new PlanilhaParser().ParseCsv(conteudo);
        }

        #endregion
    }

    /// <summary>
    /// Alias com o nome exato 'planilhaParser' conforme solicitado na issue.
    /// </summary>
    public class planilhaParser : PlanilhaParser
    {
    }

    /// <summary>
    /// Alias com o nome 'SigaaPlanilhaParser' mencionado no detalhamento da issue.
    /// </summary>
    public class SigaaPlanilhaParser : PlanilhaParser
    {
    }
}

namespace EventoWeb.Helper
{
    /// <summary>
    /// Provedor no namespace singular EventoWeb.Helper para máxima compatibilidade.
    /// </summary>
    public class PlanilhaParser : EventoWeb.Helpers.PlanilhaParser { }
    public class planilhaParser : EventoWeb.Helpers.PlanilhaParser { }
    public class SigaaPlanilhaParser : EventoWeb.Helpers.PlanilhaParser { }
}
