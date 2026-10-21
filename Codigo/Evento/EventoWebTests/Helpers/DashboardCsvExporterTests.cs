using Core.DTO;
using EventoWeb.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace EventoWeb.Helpers.Tests
{
    /// <summary>
    /// Cobertura da exportação CSV do Dashboard (Issue #856).
    /// </summary>
    [TestClass()]
    public class DashboardCsvExporterTests
    {
        private static DashboardCsvContext CriarContexto(string nomeEvento = "SEMINFO 2026") => new()
        {
            IdEvento = 7,
            NomeEvento = nomeEvento,
            Status = "A",
            IdSubevento = 3,
            IdTipoInscricao = 5,
            DataInicio = new DateTime(2026, 10, 1),
            DataFim = new DateTime(2026, 10, 31),
            GeradoEm = new DateTime(2026, 10, 21, 14, 30, 0)
        };

        private static DashboardFinanceiroDTO CriarFinanceiro() => new()
        {
            TotalInscritos = 10,
            VagasOfertadas = 20,
            PercentualOcupacao = 50,
            ReceitaConfirmada = 1234.5m,
            ValorEmAberto = 100,
            TicketMedio = 123.45m,
            PagamentosEmAberto = 2,
            Funil = new FunilInscricoesDTO
            {
                Ativas = 8,
                AtivasPercentual = 80,
                Solicitadas = 1,
                SolicitadasPercentual = 10,
                Canceladas = 1,
                CanceladasPercentual = 10
            },
            PorForma =
            {
                new FormaPagamentoDTO { Forma = "P", NomeExibicao = "Pix", Quantidade = 6, ValorTotal = 600, Percentual = 60 }
            },
            PorCategoria =
            {
                new CategoriaInscricaoDTO { Nome = "Estudante", Quantidade = 4, ValorTotal = 400, Percentual = 40 }
            }
        };

        private static DashboardOperacaoDTO CriarOperacao() => new()
        {
            Presentes = 18,
            Ausentes = 2,
            AptosCertificado = 15,
            FrequenciaMedia = 75.5m,
            OcupacaoSubeventos =
            {
                new OcupacaoSubeventoDTO { IdSubevento = 3, Nome = "Workshop", Ocupados = 5, VagasOfertadas = 10, Percentual = 50 }
            },
            PorSexo = new DistribuicaoSexoDTO { Feminino = 6, FemininoPercentual = 60, Masculino = 3, MasculinoPercentual = 30, NaoInformado = 1 },
            PorOrigem =
            {
                new OrigemGeograficaDTO { Estado = "SP", Quantidade = 7, Percentual = 70 }
            }
        };

        private static string[] Linhas(byte[] csv) =>
            Encoding.UTF8.GetString(csv)
                .TrimStart('\uFEFF')
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

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

        private static Dictionary<string, string[]> Indexar(string[] linhas) =>
            linhas.Skip(1)
                .Select(Campos)
                .ToDictionary(c => $"{c[0]}/{c[1]}/{c[2]}");

        [TestMethod()]
        public void Gerar_DeveUsarCabecalhoFixoE7ColunasEmTodasAsLinhas()
        {
            var linhas = Linhas(DashboardCsvExporter.Gerar(CriarContexto(), CriarFinanceiro(), CriarOperacao()));

            Assert.AreEqual(DashboardCsvExporter.Cabecalho, linhas[0]);
            Assert.AreEqual(7, Campos(linhas[0]).Length);
            foreach (var linha in linhas.Skip(1))
                Assert.AreEqual(7, Campos(linha).Length, $"Linha com quantidade de colunas incorreta: {linha}");
        }

        [TestMethod()]
        public void GerarLinhas_DeveRefletirAsMesmasLinhasDoCsvParaPreVisualizacao()
        {
            var contexto = CriarContexto();
            var financeiro = CriarFinanceiro();
            var operacao = CriarOperacao();

            var linhas = DashboardCsvExporter.GerarLinhas(contexto, financeiro, operacao);
            var csv = Linhas(DashboardCsvExporter.Gerar(contexto, financeiro, operacao));

            Assert.AreEqual(csv.Length, linhas.Count);
            Assert.AreEqual(DashboardCsvExporter.Cabecalho, string.Join(";", linhas[0]));

            var workshop = linhas.Single(l => l[0] == "operacao" && l[1] == "subevento" && l[2] == "Workshop");
            CollectionAssert.AreEqual(new[] { "operacao", "subevento", "Workshop", "5", "10", "", "50" }, workshop);
        }

        [TestMethod()]
        public void Gerar_DeveExportarIndicadoresCompletos()
        {
            var linhas = Linhas(DashboardCsvExporter.Gerar(CriarContexto(), CriarFinanceiro(), CriarOperacao()));
            var campos = Indexar(linhas);

            Assert.AreEqual("50", campos["financas/resumo/percentual_ocupacao"][6]);
            Assert.AreEqual("1234.50", campos["financas/resumo/receita_confirmada"][5]);
            Assert.AreEqual("2", campos["financas/resumo/pagamentos_em_aberto"][3]);
            Assert.AreEqual("80", campos["financas/funil/ativas"][6]);

            var pix = campos["financas/forma/Pix"];
            Assert.AreEqual("6", pix[3]);
            Assert.AreEqual("600.00", pix[5]);
            Assert.AreEqual("60", pix[6]);

            var estudante = campos["financas/categoria/Estudante"];
            Assert.AreEqual("4", estudante[3]);
            Assert.AreEqual("400.00", estudante[5]);
            Assert.AreEqual("40", estudante[6]);

            Assert.AreEqual("20", campos["operacao/resumo/total"][3]);

            var workshop = campos["operacao/subevento/Workshop"];
            Assert.AreEqual("5", workshop[3]);
            Assert.AreEqual("10", workshop[4]);
            Assert.AreEqual("50", workshop[6]);

            Assert.AreEqual("6", campos["operacao/sexo/feminino"][3]);
            Assert.AreEqual("10", campos["operacao/sexo/nao_informado"][6]);
            Assert.AreEqual("7", campos["operacao/origem/SP"][3]);
            Assert.AreEqual("70", campos["operacao/origem/SP"][6]);
        }

        [TestMethod()]
        public void Gerar_DeveRegistrarMetadadosEFiltrosAplicados()
        {
            var linhas = Linhas(DashboardCsvExporter.Gerar(CriarContexto(), CriarFinanceiro(), CriarOperacao()));
            var campos = Indexar(linhas);

            Assert.AreEqual("7", campos["meta/evento/id"][5]);
            Assert.AreEqual("SEMINFO 2026", campos["meta/evento/nome"][5]);
            Assert.AreEqual("A", campos["meta/evento/status"][5]);
            Assert.AreEqual("2026-10-21 14:30:00", campos["meta/exportacao/gerado_em"][5]);
            Assert.AreEqual("3", campos["meta/filtros/subevento_id"][5]);
            Assert.AreEqual("5", campos["meta/filtros/tipo_inscricao_id"][5]);
            Assert.AreEqual("2026-10-01", campos["meta/filtros/periodo_inicio"][5]);
            Assert.AreEqual("2026-10-31", campos["meta/filtros/periodo_fim"][5]);
        }

        [TestMethod()]
        public void Gerar_DeveNeutralizarInjecaoDeFormulaEmValoresExportados()
        {
            var contexto = CriarContexto("=HYPERLINK(\"http://malicioso\")");
            var financeiro = CriarFinanceiro();
            financeiro.PorCategoria.Add(new CategoriaInscricaoDTO { Nome = "+SUM(1)", Quantidade = 1, ValorTotal = 1, Percentual = 1 });
            var operacao = CriarOperacao();
            operacao.OcupacaoSubeventos.Add(new OcupacaoSubeventoDTO { Nome = "@cmd", Ocupados = 1, VagasOfertadas = 1 });

            var linhas = Linhas(DashboardCsvExporter.Gerar(contexto, financeiro, operacao));
            var campos = Indexar(linhas);

            StringAssert.StartsWith(campos["meta/evento/nome"][5], "'=");

            Assert.IsTrue(campos.TryGetValue("financas/categoria/'+SUM(1)", out var categoria));
            Assert.AreEqual("'+SUM(1)", categoria[2]);

            Assert.IsTrue(campos.TryGetValue("operacao/subevento/'@cmd", out var subevento));
            Assert.AreEqual("'@cmd", subevento[2]);
        }

        [TestMethod()]
        public void CampoCsv_DeveEscaparDelimitadorAspasEQuebraDeLinha()
        {
            Assert.AreEqual("\"a;b\"", DashboardCsvExporter.CampoCsv("a;b"));
            Assert.AreEqual("\"a\"\"b\"", DashboardCsvExporter.CampoCsv("a\"b"));
            Assert.AreEqual("\"a\nb\"", DashboardCsvExporter.CampoCsv("a\nb"));
            Assert.AreEqual("texto", DashboardCsvExporter.CampoCsv("texto"));
            Assert.AreEqual(string.Empty, DashboardCsvExporter.CampoCsv(null));
        }

        [TestMethod()]
        public void NomeArquivo_DeveConterSlugIdEData()
        {
            var nome = DashboardCsvExporter.NomeArquivo("SEMINFO 2026 - Edição Especial", 7, new DateTime(2026, 10, 21, 14, 30, 0));

            Assert.AreEqual("dashboard-seminfo-2026-edicao-especial-7-20261021-1430.csv", nome);
        }

        [TestMethod()]
        public void NomeArquivo_DeveUsarEventoQuandoNomeVazio()
        {
            var nome = DashboardCsvExporter.NomeArquivo("   ", 9, new DateTime(2026, 1, 2, 3, 4, 0));

            Assert.AreEqual("dashboard-evento-9-20260102-0304.csv", nome);
        }
    }
}
