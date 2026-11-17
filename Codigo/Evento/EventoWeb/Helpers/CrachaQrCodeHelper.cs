using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.DTO;
using Util;

namespace EventoWeb.Helpers
{
    public static class CrachaQrCodeHelper
    {
        public const int VersaoAtual = 1;
        public const int TamanhoTruncamentoHash = 8;
        public const string SaltoPadraoChave = "borala:evento";

        /// <summary>
        /// Obtém a chave padrão derivada para o evento caso nenhuma chave customizada seja fornecida.
        /// </summary>
        public static string ObterChavePadraoEvento(uint idEvento, string? salt = null)
        {
            var prefixo = string.IsNullOrWhiteSpace(salt) ? SaltoPadraoChave : salt.Trim();
            return $"{prefixo}:{idEvento}:secret";
        }

        /// <summary>
        /// Sanitiza o CPF mantendo apenas os 11 dígitos numéricos.
        /// </summary>
        public static string SanitizarCpf(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return "00000000000";

            var apenasDigitos = Regex.Replace(cpf, @"\D", "");
            if (apenasDigitos.Length > 11)
                apenasDigitos = apenasDigitos.Substring(0, 11);
            else if (apenasDigitos.Length < 11)
                apenasDigitos = apenasDigitos.PadLeft(11, '0');

            return apenasDigitos;
        }

        /// <summary>
        /// Gera a string canônica determinística para cálculo da assinatura HMAC-SHA256:
        /// {v}|{pid}|{cpf}|{nome}|{eid}|{sid_ordenados_separados_por_virgula}
        /// </summary>
        public static string ObterStringCanonizada(
            int versao,
            uint idPessoa,
            string? cpf,
            string? nome,
            uint idEvento,
            IEnumerable<uint>? subeventosIds)
        {
            var cpfLimpo = SanitizarCpf(cpf);
            var nomeLimpo = (nome ?? string.Empty).Trim();
            var sidsOrdenados = (subeventosIds ?? Enumerable.Empty<uint>())
                .Distinct()
                .OrderBy(id => id);

            return $"{versao}|{idPessoa}|{cpfLimpo}|{nomeLimpo}|{idEvento}|{string.Join(",", sidsOrdenados)}";
        }

        /// <summary>
        /// Calcula a assinatura HMAC-SHA256 truncada em 8 caracteres hexadecimais minúsculos.
        /// </summary>
        public static string CalcularHash(
            int versao,
            uint idPessoa,
            string? cpf,
            string? nome,
            uint idEvento,
            IEnumerable<uint>? subeventosIds,
            string? secretKey = null)
        {
            var chave = string.IsNullOrWhiteSpace(secretKey)
                ? ObterChavePadraoEvento(idEvento)
                : secretKey;

            var canonical = ObterStringCanonizada(versao, idPessoa, cpf, nome, idEvento, subeventosIds);
            var keyBytes = Encoding.UTF8.GetBytes(chave);
            var dataBytes = Encoding.UTF8.GetBytes(canonical);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            var hex = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return hex.Length >= TamanhoTruncamentoHash
                ? hex.Substring(0, TamanhoTruncamentoHash)
                : hex;
        }

        /// <summary>
        /// Constrói o objeto CrachaQrPayloadDto completo com hash assinado.
        /// </summary>
        public static CrachaQrPayloadDto GerarPayload(
            uint idPessoa,
            string? cpf,
            string? nome,
            uint idEvento,
            IEnumerable<uint>? subeventosIds,
            string? secretKey = null,
            int versao = VersaoAtual)
        {
            var chave = string.IsNullOrWhiteSpace(secretKey)
                ? ObterChavePadraoEvento(idEvento)
                : secretKey;

            var sids = (subeventosIds ?? Enumerable.Empty<uint>())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var hash = CalcularHash(versao, idPessoa, cpf, nome, idEvento, sids, chave);

            return new CrachaQrPayloadDto
            {
                V = versao,
                Pid = idPessoa,
                Cpf = SanitizarCpf(cpf),
                Nome = (nome ?? string.Empty).Trim(),
                Eid = idEvento,
                Sid = sids,
                Hash = hash
            };
        }

        /// <summary>
        /// Serializa o payload para JSON compacto.
        /// </summary>
        public static string GerarPayloadJson(
            uint idPessoa,
            string? cpf,
            string? nome,
            uint idEvento,
            IEnumerable<uint>? subeventosIds,
            string? secretKey = null,
            int versao = VersaoAtual)
        {
            var payload = GerarPayload(idPessoa, cpf, nome, idEvento, subeventosIds, secretKey, versao);
            return JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Valida a integridade do JSON e da assinatura HMAC.
        /// </summary>
        public static bool ValidarPayload(
            string? rawJson,
            string? secretKey,
            out CrachaQrPayloadDto? payload,
            out string? erro)
        {
            payload = null;
            erro = null;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                erro = "QR Code vazio.";
                return false;
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<CrachaQrPayloadDto>(rawJson.Trim(), options);

                if (parsed == null)
                {
                    erro = "QR Code com estrutura inválida.";
                    return false;
                }

                if (parsed.V != VersaoAtual)
                {
                    erro = $"Versão do QR Code não suportada: {parsed.V}.";
                    return false;
                }

                if (parsed.Pid == 0)
                {
                    erro = "Identificador de participante (pid) inválido.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(parsed.Nome))
                {
                    erro = "Nome do participante ausente no QR Code.";
                    return false;
                }

                if (parsed.Eid == 0)
                {
                    erro = "Identificador de evento (eid) inválido.";
                    return false;
                }

                var chave = string.IsNullOrWhiteSpace(secretKey)
                    ? ObterChavePadraoEvento(parsed.Eid)
                    : secretKey;

                var hashEsperado = CalcularHash(parsed.V, parsed.Pid, parsed.Cpf, parsed.Nome, parsed.Eid, parsed.Sid, chave);
                if (!string.Equals(parsed.Hash, hashEsperado, StringComparison.OrdinalIgnoreCase))
                {
                    erro = "Assinatura do QR Code inválida (dados adulterados).";
                    return false;
                }

                parsed.Cpf = SanitizarCpf(parsed.Cpf);
                parsed.Nome = parsed.Nome.Trim();
                parsed.Sid = (parsed.Sid ?? new List<uint>())
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();
                parsed.Hash = hashEsperado;

                payload = parsed;
                return true;
            }
            catch (JsonException ex)
            {
                erro = $"JSON do QR Code malformado: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                erro = $"Erro na validação do QR Code: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Gera a imagem do QR Code em Base64 (PNG) a partir do payload JSON assinado.
        /// </summary>
        public static string GerarQrCodeBase64(
            uint idPessoa,
            string? cpf,
            string? nome,
            uint idEvento,
            IEnumerable<uint>? subeventosIds,
            string? secretKey = null,
            int versao = VersaoAtual)
        {
            try
            {
                var json = GerarPayloadJson(idPessoa, cpf, nome, idEvento, subeventosIds, secretKey, versao);
                var qrBytes = QrCodeGenerator.GenerateQr(json);
                return Convert.ToBase64String(qrBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Converte formato textual legado "[IdPessoa] [Nome] [IdEvento] [IdSub1]..." em CrachaQrPayloadDto
        /// </summary>
        public static bool TentarConverterDeLegado(
            string? textoLegado,
            uint idEventoPadrao,
            string? secretKey,
            out CrachaQrPayloadDto? payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(textoLegado))
                return false;

            var trimmed = textoLegado.Trim();
            if (!trimmed.StartsWith("["))
                return false;

            var matches = Regex.Matches(trimmed, @"\[(.*?)\]");
            if (matches.Count < 3)
                return false;

            if (!uint.TryParse(matches[0].Groups[1].Value, out var pid))
                return false;

            var nome = matches[1].Groups[1].Value.Trim();
            if (!uint.TryParse(matches[2].Groups[1].Value, out var eid))
                eid = idEventoPadrao;

            var sids = new List<uint>();
            for (int i = 3; i < matches.Count; i++)
            {
                if (uint.TryParse(matches[i].Groups[1].Value, out var sid))
                    sids.Add(sid);
            }

            payload = GerarPayload(pid, "00000000000", nome, eid, sids, secretKey);
            return true;
        }
    }
}

