using System.Collections.Generic;
using Core.DTO;
using EventoWeb.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventoWebTests.Helpers
{
    [TestClass]
    public class CrachaQrCodeHelperTests
    {
        private const string ChaveTeste = "chave_teste_123";

        [TestMethod]
        public void CalcularHash_GoldenVector_GeraExatamenteMesmoHashQueFlutterDart()
        {
            // Arrange: Vetor Golden compartilhado entre Boralá (C#) e Bipou (Dart)
            int v = 1;
            uint pid = 1042;
            string cpf = "12345678901";
            string nome = "Maria Santos";
            uint eid = 15;
            var sid = new List<uint> { 42, 43 };

            // Act
            var hash = CrachaQrCodeHelper.CalcularHash(v, pid, cpf, nome, eid, sid, ChaveTeste);

            // Assert: Deve bater exatamente com "58663eab"
            Assert.AreEqual("58663eab", hash);
        }

        [TestMethod]
        public void CalcularHash_SubeventosDesordenadosEDuplicadosECpfFormatado_GeraMesmoHashCanonizado()
        {
            // Arrange
            int v = 1;
            uint pid = 1042;
            string cpfFormatado = "123.456.789-01";
            string nomeComEspacos = "  Maria Santos  ";
            uint eid = 15;
            var sidDesordenado = new List<uint> { 43, 42, 42, 43 };

            // Act
            var hash = CrachaQrCodeHelper.CalcularHash(v, pid, cpfFormatado, nomeComEspacos, eid, sidDesordenado, ChaveTeste);

            // Assert
            Assert.AreEqual("58663eab", hash);
        }

        [TestMethod]
        public void CalcularHash_DadosAlterados_GeraHashDiferente()
        {
            // Arrange
            var sid = new List<uint> { 42, 43 };
            var hashOriginal = CrachaQrCodeHelper.CalcularHash(1, 1042, "12345678901", "Maria Santos", 15, sid, ChaveTeste);

            // Act: Alteração de participante (pid)
            var hashAdulterado = CrachaQrCodeHelper.CalcularHash(1, 9999, "12345678901", "Maria Santos", 15, sid, ChaveTeste);

            // Assert
            Assert.AreNotEqual(hashOriginal, hashAdulterado);
        }

        [TestMethod]
        public void GerarPayloadJson_GeraJsonCompactoComCamposEsperados()
        {
            // Arrange & Act
            var json = CrachaQrCodeHelper.GerarPayloadJson(
                idPessoa: 1042,
                cpf: "12345678901",
                nome: "Maria Santos",
                idEvento: 15,
                subeventosIds: new List<uint> { 42, 43 },
                secretKey: ChaveTeste);

            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(json));
            Assert.IsTrue(json.Contains("\"v\":1"));
            Assert.IsTrue(json.Contains("\"pid\":1042"));
            Assert.IsTrue(json.Contains("\"cpf\":\"12345678901\""));
            Assert.IsTrue(json.Contains("\"nome\":\"Maria Santos\""));
            Assert.IsTrue(json.Contains("\"eid\":15"));
            Assert.IsTrue(json.Contains("\"sid\":[42,43]"));
            Assert.IsTrue(json.Contains("\"hash\":\"58663eab\""));
        }

        [TestMethod]
        public void ValidarPayload_PayloadValidoEChaveCorreta_RetornaSucessoEObjetoPreenchido()
        {
            // Arrange
            var json = CrachaQrCodeHelper.GerarPayloadJson(
                idPessoa: 1042,
                cpf: "12345678901",
                nome: "Maria Santos",
                idEvento: 15,
                subeventosIds: new List<uint> { 42, 43 },
                secretKey: ChaveTeste);

            // Act
            var sucesso = CrachaQrCodeHelper.ValidarPayload(json, ChaveTeste, out var payload, out var erro);

            // Assert
            Assert.IsTrue(sucesso);
            Assert.IsNull(erro);
            Assert.IsNotNull(payload);
            Assert.AreEqual(1, payload.V);
            Assert.AreEqual((uint)1042, payload.Pid);
            Assert.AreEqual("12345678901", payload.Cpf);
            Assert.AreEqual("Maria Santos", payload.Nome);
            Assert.AreEqual((uint)15, payload.Eid);
            Assert.AreEqual(2, payload.Sid.Count);
            Assert.AreEqual("58663eab", payload.Hash);
        }

        [TestMethod]
        public void ValidarPayload_DadosAdulterados_RetornaFalseEAcusaErroDeAssinatura()
        {
            // Arrange: JSON com payload forjado (pid alterado para 9999 mantendo hash antigo)
            const string jsonAdulterado = "{\"v\":1,\"pid\":9999,\"cpf\":\"12345678901\",\"nome\":\"Maria Santos\",\"eid\":15,\"sid\":[42,43],\"hash\":\"58663eab\"}";

            // Act
            var sucesso = CrachaQrCodeHelper.ValidarPayload(jsonAdulterado, ChaveTeste, out var payload, out var erro);

            // Assert
            Assert.IsFalse(sucesso);
            Assert.IsNull(payload);
            Assert.IsNotNull(erro);
            Assert.IsTrue(erro.Contains("Assinatura do QR Code inválida (dados adulterados)"));
        }

        [TestMethod]
        public void ValidarPayload_JsonInvalidoOuCamposObrigatoriosAusentes_RetornaFalseComDescricao()
        {
            // QR Code vazio
            Assert.IsFalse(CrachaQrCodeHelper.ValidarPayload("", ChaveTeste, out _, out var erroVazio));
            Assert.IsTrue(erroVazio?.Contains("vazio") == true);

            // JSON quebrado
            Assert.IsFalse(CrachaQrCodeHelper.ValidarPayload("{not a json}", ChaveTeste, out _, out var erroJson));
            Assert.IsTrue(erroJson?.Contains("malformado") == true);

            // Versão não suportada
            Assert.IsFalse(CrachaQrCodeHelper.ValidarPayload("{\"v\":2,\"pid\":1,\"nome\":\"A\",\"eid\":1,\"hash\":\"abc\"}", ChaveTeste, out _, out var erroVersao));
            Assert.IsTrue(erroVersao?.Contains("não suportada") == true);

            // Nome ausente
            Assert.IsFalse(CrachaQrCodeHelper.ValidarPayload("{\"v\":1,\"pid\":1,\"nome\":\"\",\"eid\":1,\"hash\":\"abc\"}", ChaveTeste, out _, out var erroNome));
            Assert.IsTrue(erroNome?.Contains("Nome do participante ausente") == true);
        }

        [TestMethod]
        public void GerarQrCodeBase64_GeraStringNaoVaziaEValida()
        {
            // Act
            var qrCodeBase64 = CrachaQrCodeHelper.GerarQrCodeBase64(
                idPessoa: 1042,
                cpf: "12345678901",
                nome: "Maria Santos",
                idEvento: 15,
                subeventosIds: new List<uint> { 42, 43 },
                secretKey: ChaveTeste);

            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(qrCodeBase64));
            // Deve ser Base64 decodificável
            var bytes = System.Convert.FromBase64String(qrCodeBase64);
            Assert.IsTrue(bytes.Length > 0);
        }

        [TestMethod]
        public void TentarConverterDeLegado_TextoColchetes_ConverteComSucesso()
        {
            // Arrange
            const string textoLegado = "[1042] [Maria Santos] [15] [42] [43]";

            // Act
            var sucesso = CrachaQrCodeHelper.TentarConverterDeLegado(textoLegado, 15, ChaveTeste, out var payload);

            // Assert
            Assert.IsTrue(sucesso);
            Assert.IsNotNull(payload);
            Assert.AreEqual((uint)1042, payload.Pid);
            Assert.AreEqual("Maria Santos", payload.Nome);
            Assert.AreEqual((uint)15, payload.Eid);
            Assert.AreEqual(2, payload.Sid.Count);
            Assert.AreEqual((uint)42, payload.Sid[0]);
            Assert.AreEqual((uint)43, payload.Sid[1]);
        }

        [TestMethod]
        public void ObterChavePadraoEvento_GeraChavePadraoDerivadaEstavel()
        {
            var chave1 = CrachaQrCodeHelper.ObterChavePadraoEvento(15);
            var chave2 = CrachaQrCodeHelper.ObterChavePadraoEvento(15);
            var chaveCustom = CrachaQrCodeHelper.ObterChavePadraoEvento(15, "outro:salt");

            Assert.AreEqual("borala:evento:15:secret", chave1);
            Assert.AreEqual(chave1, chave2);
            Assert.AreEqual("outro:salt:15:secret", chaveCustom);
        }

        [TestMethod]
        public void GerarArquivosDeImagemQrCodeParaTesteVisual()
        {
            var pastaArtifacts = @"C:\Users\SANDRO\.gemini\antigravity\brain\e36ee904-d8f5-42df-b112-309e4c456830";
            if (!System.IO.Directory.Exists(pastaArtifacts))
            {
                System.IO.Directory.CreateDirectory(pastaArtifacts);
            }

            // 1. QR Code Válido (Golden Vector - Maria Santos)
            var jsonValido = CrachaQrCodeHelper.GerarPayloadJson(
                idPessoa: 1042,
                cpf: "12345678901",
                nome: "Maria Santos",
                idEvento: 15,
                subeventosIds: new List<uint> { 42, 43 },
                secretKey: "chave_teste_123");
            var bytesValido = Util.QrCodeGenerator.GenerateQr(jsonValido);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(pastaArtifacts, "qrcode_valido_maria_santos.png"), bytesValido);

            // 2. QR Code Adulterado (Tentativa de Fraude - ID alterado para 9999 mantendo hash antigo)
            const string jsonAdulterado = "{\"v\":1,\"pid\":9999,\"cpf\":\"12345678901\",\"nome\":\"Maria Santos\",\"eid\":15,\"sid\":[42,43],\"hash\":\"58663eab\"}";
            var bytesAdulterado = Util.QrCodeGenerator.GenerateQr(jsonAdulterado);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(pastaArtifacts, "qrcode_adulterado_fraude.png"), bytesAdulterado);

            // 3. QR Code Demo Boralá (Cassiano Menezes - Evento 1)
            var jsonCassiano = CrachaQrCodeHelper.GerarPayloadJson(
                idPessoa: 1,
                cpf: "04299988818",
                nome: "Cassiano Menezes",
                idEvento: 1,
                subeventosIds: new List<uint>());
            var bytesCassiano = Util.QrCodeGenerator.GenerateQr(jsonCassiano);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(pastaArtifacts, "qrcode_borala_cassiano.png"), bytesCassiano);
        }
    }
}

