using EventoWeb.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace EventoWeb.Helpers.Tests
{
    /// <summary>
    /// Cobertura da exportação CSV da tela de frequência (Issue #880).
    /// </summary>
    [TestClass()]
    public class FrequenciaCsvExporterTests
    {
        private static FrequenciaCsvItem CriarItem(
            string nome = "Maria Silva",
            string cpf = "12345678909",
            string email = "maria@teste.com",
            DateTime? entrada = null,
            DateTime? saida = null) => new()
        {
            Nome = nome,
            Cpf = cpf,
            Email = email,
            Entrada = entrada ?? new DateTime(2026, 10, 30, 9, 0, 0),
            Saida = saida
        };

        private static string[] Linhas(byte[] csv) =>
            Encoding.UTF8.GetString(csv)
                .TrimStart('\uFEFF')
                .TrimEnd('\r', '\n')
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        private static string[] Campos(string linha)
        {
            var campos = new List<string>();
            var atual = new StringBuilder();
            var entreAspas = false;

            for (var i = 0; i < linha.Length; i++)
            {
                var c = linha[i];
                if (entreAspas)
                {
                    if (c != '"')
                    {
                        atual.Append(c);
                    }
                    else if (i + 1 < linha.Length && linha[i + 1] == '"')
                    {
                        atual.Append('"');
                        i++;
                    }
                    else
                    {
                        entreAspas = false;
                    }
                }
                else if (c == '"')
                {
                    entreAspas = true;
                }
                else if (c == ';')
                {
                    campos.Add(atual.ToString());
                    atual.Clear();
                }
                else
                {
                    atual.Append(c);
                }
            }

            campos.Add(atual.ToString());
            return campos.ToArray();
        }

        [TestMethod()]
        public void Gerar_DeveUsarTituloELayoutDaPlanilhaMoodle()
        {
            var csv = FrequenciaCsvExporter.Gerar("SEMAC 2026", null, new[] { CriarItem() });
            var linhas = Linhas(csv);

            Assert.AreEqual("CPF;NOME;SOBRENOME;E-MAIL;TIPO DA PARTICIPAÇÃO;ENTRADA;SAIDA;PERMANENCIA_MINUTOS", FrequenciaCsvExporter.Cabecalho);
            Assert.AreEqual("TÍTULO: SEMAC 2026", linhas[0]);
            Assert.AreEqual(string.Empty, linhas[1]);
            Assert.AreEqual(FrequenciaCsvExporter.Cabecalho, linhas[2]);
            Assert.AreEqual(8, Campos(linhas[2]).Length);
            Assert.AreEqual(8, Campos(linhas[3]).Length);
        }

        [TestMethod()]
        public void Gerar_EventoSemSubEvento_PreencheNomeSobrenomeETipo()
        {
            var csv = FrequenciaCsvExporter.Gerar("SEMAC 2026", null, new[] { CriarItem() });

            var campos = Campos(Linhas(csv)[3]);

            CollectionAssert.AreEqual(
                new[] { "12345678909", "Maria", "Silva", "maria@teste.com", "PARTICIPANTE", "30/10/2026 09:00", "", "" },
                campos);
        }

        [TestMethod()]
        public void Gerar_ComSubEvento_UsaTituloCompletoECalculaPermanencia()
        {
            var item = CriarItem(
                entrada: new DateTime(2026, 10, 30, 9, 0, 0),
                saida: new DateTime(2026, 10, 30, 10, 30, 0));

            var csv = FrequenciaCsvExporter.Gerar("SEMAC 2026", "Workshop .NET", new[] { item });
            var linhas = Linhas(csv);
            var campos = Campos(linhas[3]);

            Assert.AreEqual("TÍTULO: SEMAC 2026 - Workshop .NET", linhas[0]);
            Assert.AreEqual("30/10/2026 09:00", campos[5]);
            Assert.AreEqual("30/10/2026 10:30", campos[6]);
            Assert.AreEqual("90", campos[7]);
        }

        [TestMethod()]
        public void Gerar_SemSaida_DeixaSaidaEPermanenciaVazias()
        {
            var csv = FrequenciaCsvExporter.Gerar("SEMAC 2026", null, new[] { CriarItem() });
            var campos = Campos(Linhas(csv)[3]);

            Assert.AreEqual(string.Empty, campos[6]);
            Assert.AreEqual(string.Empty, campos[7]);
        }

        [TestMethod()]
        public void Gerar_SaidaAntesDaEntrada_NaoGeraPermanenciaNegativa()
        {
            var item = CriarItem(
                entrada: new DateTime(2026, 10, 30, 10, 0, 0),
                saida: new DateTime(2026, 10, 30, 9, 0, 0));

            var csv = FrequenciaCsvExporter.Gerar("SEMAC 2026", null, new[] { item });

            Assert.AreEqual("0", Campos(Linhas(csv)[3])[7]);
        }

        [TestMethod()]
        public void Gerar_ListaVazia_RetornaTituloLinhaEmBrancoECabecalhoComBom()
        {
            var csv = FrequenciaCsvExporter.Gerar("SEMAC 2026", null, Array.Empty<FrequenciaCsvItem>());
            var linhas = Linhas(csv);

            CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, csv.Take(3).ToArray());
            Assert.AreEqual(3, linhas.Length);
            Assert.AreEqual("TÍTULO: SEMAC 2026", linhas[0]);
            Assert.AreEqual(string.Empty, linhas[1]);
            Assert.AreEqual(FrequenciaCsvExporter.Cabecalho, linhas[2]);
        }

        [TestMethod()]
        public void Gerar_DeveEscaparDelimitadorAspasEQuebraDeLinha()
        {
            var item = CriarItem(nome: "Ana;Maria \"X\" Silva", cpf: "1;2", email: "ana@teste.com");

            var csv = FrequenciaCsvExporter.Gerar("SEMAC 2026", null, new[] { item });
            var linha = Linhas(csv)[3];

            StringAssert.Contains(linha, "\"Ana;Maria\";\"\"\"X\"\" Silva\"");
            var campos = Campos(linha);
            Assert.AreEqual("1;2", campos[0]);
            Assert.AreEqual("Ana;Maria", campos[1]);
            Assert.AreEqual("\"X\" Silva", campos[2]);

            Assert.AreEqual("\"a\r\nb\"", CsvFormat.Campo("a\r\nb"));
        }

        [TestMethod()]
        public void Gerar_DeveNeutralizarInjecaoDeFormula()
        {
            var itens = new[]
            {
                CriarItem(nome: "=1+1"),
                CriarItem(nome: "+SUM(A1)"),
                CriarItem(nome: "-2+3"),
                CriarItem(nome: "@cmd")
            };

            var csv = FrequenciaCsvExporter.Gerar("=HYPERLINK(\"http://malicioso\")", null, itens);
            var linhas = Linhas(csv);
            var dados = linhas.Skip(3).Select(Campos).ToArray();

            StringAssert.Contains(linhas[0], "TÍTULO: =HYPERLINK");
            StringAssert.StartsWith(dados[0][1], "'=");
            StringAssert.StartsWith(dados[1][1], "'+");
            StringAssert.StartsWith(dados[2][1], "'-");
            StringAssert.StartsWith(dados[3][1], "'@");
        }

        [TestMethod()]
        public void DividirNome_DeveSepararPrimeiroNomeDoSobrenome()
        {
            var (nome, sobrenome) = FrequenciaCsvExporter.DividirNome("Maria Silva");
            Assert.AreEqual("Maria", nome);
            Assert.AreEqual("Silva", sobrenome);

            (nome, sobrenome) = FrequenciaCsvExporter.DividirNome("Maria");
            Assert.AreEqual("Maria", nome);
            Assert.AreEqual(string.Empty, sobrenome);

            (nome, sobrenome) = FrequenciaCsvExporter.DividirNome("   ");
            Assert.AreEqual(string.Empty, nome);
            Assert.AreEqual(string.Empty, sobrenome);
        }

        [TestMethod()]
        public void NomeArquivo_Evento_DeveConterSlugIdEData()
        {
            var nome = FrequenciaCsvExporter.NomeArquivo(
                "SEMAC 2026 - Edição Especial", 7, null, null, new DateTime(2026, 10, 21, 14, 30, 0));

            Assert.AreEqual("frequencia-semac-2026-edicao-especial-7-20261021-1430.csv", nome);
        }

        [TestMethod()]
        public void NomeArquivo_SubEvento_DeveConterSlugDoSubEvento()
        {
            var nome = FrequenciaCsvExporter.NomeArquivo(
                "SEMAC 2026", 7, "Workshop .NET", 3, new DateTime(2026, 10, 21, 14, 30, 0));

            Assert.AreEqual("frequencia-semac-2026-7-workshop-net-3-20261021-1430.csv", nome);
        }

        [TestMethod()]
        public void NomeArquivo_NomeVazio_DeveUsarFallback()
        {
            var nome = FrequenciaCsvExporter.NomeArquivo(
                "   ", 9, null, null, new DateTime(2026, 1, 2, 3, 4, 0));

            Assert.AreEqual("frequencia-evento-9-20260102-0304.csv", nome);
        }
    }
}
