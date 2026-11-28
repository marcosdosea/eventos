using ClosedXML.Excel;
using EventoWeb.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventoWeb.Helpers.Tests
{
    [TestClass]
    public class FrequenciaExcelExporterTests
    {
        private static FrequenciaRelatorioItem CriarItem(
            string nome = "Maria Santos",
            string cpf = "123.456.789-00",
            string email = "maria@teste.com",
            string curso = "Workshop .NET",
            decimal cargaHoraria = 20m,
            decimal horasPresentes = 16m,
            decimal percentual = 80m,
            string situacao = "APTO PARA CERTIFICADO",
            string historico = "18/11/2026 08:00 às 12:00") => new()
        {
            NomeCompleto = nome,
            Cpf = cpf,
            Email = email,
            CursoOuSubevento = curso,
            CargaHorariaTotal = cargaHoraria,
            TotalHorasPresentes = horasPresentes,
            PercentualFrequencia = percentual,
            SituacaoFrequencia = situacao,
            HistoricoEntradasSaidas = historico
        };

        [TestMethod]
        public void Gerar_DeveGerarPlanilhaXlsxCom9ColunasEFormatacaoCondicional()
        {
            var itens = new List<FrequenciaRelatorioItem>
            {
                CriarItem("Aluno Apto", situacao: "APTO PARA CERTIFICADO", percentual: 85m),
                CriarItem("Aluno Reprovado", situacao: "INSUFICIENTE", percentual: 50m)
            };

            var bytes = FrequenciaExcelExporter.Gerar("SEMAC 2026", "Palestra IA", itens);

            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);

            using var ms = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheet("Frequência");

            Assert.IsNotNull(ws);
            Assert.AreEqual("TÍTULO: SEMAC 2026 - Palestra IA", ws.Cell(1, 1).GetString());

            // Cabeçalhos na linha 3
            Assert.AreEqual(9, FrequenciaExcelExporter.Cabecalhos.Length);
            for (int col = 0; col < FrequenciaExcelExporter.Cabecalhos.Length; col++)
            {
                Assert.AreEqual(FrequenciaExcelExporter.Cabecalhos[col], ws.Cell(3, col + 1).GetString());
            }

            // Linha 4: Aluno Apto
            Assert.AreEqual("Aluno Apto", ws.Cell(4, 2).GetString());
            Assert.AreEqual("APTO PARA CERTIFICADO", ws.Cell(4, 8).GetString());
            Assert.AreEqual(XLColor.FromArgb(212, 237, 218), ws.Cell(4, 8).Style.Fill.BackgroundColor);

            // Linha 5: Aluno Reprovado
            Assert.AreEqual("Aluno Reprovado", ws.Cell(5, 2).GetString());
            Assert.AreEqual("INSUFICIENTE", ws.Cell(5, 8).GetString());
            Assert.AreEqual(XLColor.FromArgb(248, 215, 218), ws.Cell(5, 8).Style.Fill.BackgroundColor);
        }

        [TestMethod]
        public void Gerar_ListaVazia_GeraCabecalhosEArquivoValido()
        {
            var bytes = FrequenciaExcelExporter.Gerar("Evento Geral", null, Array.Empty<FrequenciaRelatorioItem>());

            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);

            using var ms = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheet("Frequência");

            Assert.AreEqual("TÍTULO: Evento Geral", ws.Cell(1, 1).GetString());
            Assert.AreEqual("CPF", ws.Cell(3, 1).GetString());
            Assert.AreEqual("Histórico de Entradas e Saídas", ws.Cell(3, 9).GetString());
        }

        [TestMethod]
        public void NomeArquivo_GeraNomeCorretoComExtensaoXlsx()
        {
            var data = new DateTime(2026, 11, 25, 14, 30, 0);
            var nome = FrequenciaExcelExporter.NomeArquivo("SEMAC 2026", 10, "Minicurso C#", 25, data);

            Assert.IsTrue(nome.EndsWith(".xlsx"));
            Assert.IsTrue(nome.Contains("frequencia"));
            Assert.IsTrue(nome.Contains("10"));
            Assert.IsTrue(nome.Contains("25"));
            Assert.IsTrue(nome.Contains("20261125-1430"));
        }
    }
}
