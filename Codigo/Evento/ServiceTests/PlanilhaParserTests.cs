using System.Text;
using ClosedXML.Excel;
using Core.DTO;
using EventoWeb.Helpers;
using EventoWeb.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Util;

namespace ServiceTests
{
    [TestClass]
    public class PlanilhaParserTests
    {
        private PlanilhaParser _parser = null!;

        // CPFs válidos conhecidos para testes
        private const string CpfValidoFormatado1 = "040.268.930-57";
        private const string CpfValidoNaoFormatado1 = "04026893057";
        private const string CpfValidoFormatado2 = "917.091.250-55";
        private const string CpfValidoNaoFormatado2 = "91709125055";
        private const string CpfValidoFormatado3 = "206.015.300-04";
        private const string CpfValidoComZeroPerdido = "4026893057"; // 10 dígitos (Excel truncando 0 inicial de 04026893057)

        [TestInitialize]
        public void Setup()
        {
            _parser = new PlanilhaParser();
        }

        #region Testes de CpfValidator

        [TestMethod]
        public void CpfValidator_CpfValidoFormatado_DeveRetornarVerdadeiro()
        {
            Assert.IsTrue(CpfValidator.Validar(CpfValidoFormatado1));
            Assert.IsTrue(CpfValidator.Validar(CpfValidoFormatado2));
            Assert.IsTrue(CpfValidator.Validar(CpfValidoFormatado3));
        }

        [TestMethod]
        public void CpfValidator_CpfValidoNaoFormatado_DeveRetornarVerdadeiro()
        {
            Assert.IsTrue(CpfValidator.Validar(CpfValidoNaoFormatado1));
            Assert.IsTrue(CpfValidator.Validar(CpfValidoNaoFormatado2));
        }

        [TestMethod]
        public void CpfValidator_CpfComZeroPerdido_DeveValidarENormalizar()
        {
            Assert.IsTrue(CpfValidator.Validar(CpfValidoComZeroPerdido));
            var normalizado = CpfValidator.Normalizar(CpfValidoComZeroPerdido);
            Assert.AreEqual("04026893057", normalizado);
        }

        [TestMethod]
        public void CpfValidator_CpfInvalido_DigitosVerificadoresIncorretos_DeveRetornarFalso()
        {
            Assert.IsFalse(CpfValidator.Validar("123.456.789-00"));
            Assert.IsFalse(CpfValidator.Validar("040.268.930-99"));
        }

        [TestMethod]
        public void CpfValidator_CpfInvalido_DigitosRepetidos_DeveRetornarFalso()
        {
            Assert.IsFalse(CpfValidator.Validar("111.111.111-11"));
            Assert.IsFalse(CpfValidator.Validar("000.000.000-00"));
            Assert.IsFalse(CpfValidator.Validar("99999999999"));
        }

        [TestMethod]
        public void CpfValidator_CpfVazioOuNulo_DeveRetornarFalso()
        {
            Assert.IsFalse(CpfValidator.Validar(null));
            Assert.IsFalse(CpfValidator.Validar(""));
            Assert.IsFalse(CpfValidator.Validar("   "));
        }

        [TestMethod]
        public void CpfValidator_Normalizar_DeveRemoverPontosETraços()
        {
            var resultado = CpfValidator.Normalizar(" 040.268.930-57 ");
            Assert.AreEqual("04026893057", resultado);
        }

        #endregion

        #region Testes de Parsing CSV

        [TestMethod]
        public void ParseCsv_SeparadoPorPontoEVirgula_DeveProcessarComSucesso()
        {
            var csv = @"CPF;Nome Completo;E-mail;Tipo de Participação
040.268.930-57;Carlos Eduardo;carlos@email.com;Ouvinte
917.091.250-55;Ana Beatriz;ana@email.com;Palestrante";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(2, resultado.ListaSucesso.Count);
            Assert.AreEqual(0, resultado.ListaErros.Count);
            Assert.IsTrue(resultado.Sucesso);

            Assert.AreEqual("04026893057", resultado.ListaSucesso[0].Cpf);
            Assert.AreEqual("Carlos Eduardo", resultado.ListaSucesso[0].Nome);
            Assert.AreEqual("carlos@email.com", resultado.ListaSucesso[0].Email);
            Assert.AreEqual("Ouvinte", resultado.ListaSucesso[0].TipoParticipacao);
            Assert.AreEqual("Ouvinte", resultado.ListaSucesso[0].TipoParticipação);
        }

        [TestMethod]
        public void ParseCsv_SeparadoPorVirgula_DeveProcessarComSucesso()
        {
            var csv = @"CPF,Nome Completo,E-mail,Tipo Participação
04026893057,João da Silva,joao@ufs.br,Estudante
91709125055,Maria Souza,maria@ufs.br,Docente";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(2, resultado.ListaSucesso.Count);
            Assert.AreEqual(0, resultado.ListaErros.Count);
            Assert.AreEqual("João da Silva", resultado.ListaSucesso[0].Nome);
            Assert.AreEqual("04026893057", resultado.ListaSucesso[0].Cpf);
            Assert.AreEqual("Estudante", resultado.ListaSucesso[0].TipoParticipacao);
        }

        [TestMethod]
        public void ParseCsv_ComMetadadosDeEmissaoSIGAA_DeveIgnorarLinhasAdicionais()
        {
            var csv = @"UNIVERSIDADE FEDERAL DE SERGIPE
SISTEMA INTEGRADO DE GESTÃO DE ATIVIDADES ACADÊMICAS - SIGAA
RELATÓRIO DE PARTICIPANTES INSCRITOS
Emitido em 06/11/2026 às 16:30 por COORDENAÇÃO DE EXTENSÃO

CPF;Nome Completo;E-mail;Categoria
040.268.930-57;Luciana Lima;luciana@ufs.br;Participante
917.091.250-55;Pedro Henrique;pedro@ufs.br;Comissão";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(2, resultado.ListaSucesso.Count);
            Assert.AreEqual(0, resultado.ListaErros.Count);
            Assert.AreEqual("Luciana Lima", resultado.ListaSucesso[0].Nome);
            Assert.AreEqual("04026893057", resultado.ListaSucesso[0].Cpf);
        }

        [TestMethod]
        public void ParseCsv_ComLinhasVazias_DeveIgnorarSemErros()
        {
            var csv = @"CPF;Nome;Email;Tipo

040.268.930-57;João Silva;joao@teste.com;Ouvinte


917.091.250-55;Maria Santos;maria@teste.com;Monitor

";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(2, resultado.ListaSucesso.Count);
            Assert.AreEqual(0, resultado.ListaErros.Count);
        }

        [TestMethod]
        public void ParseCsv_CodificadoEmUtf8ComCaracteresAcentuados_DevePreservarCaracteres()
        {
            var csv = "CPF;Nome Completo;E-mail;Tipo de Participação\n" +
                      "040.268.930-57;João Gonçalves Müller;joao.muller@ufs.br;Coordenação\n" +
                      "917.091.250-55;Conceição Esperança;conceicao@ufs.br;Apresentação";

            var bytesUtf8 = Encoding.UTF8.GetBytes(csv);
            using var ms = new MemoryStream(bytesUtf8);

            var resultado = _parser.Parse(ms, "inscritos_utf8.csv");

            Assert.AreEqual(2, resultado.ListaSucesso.Count);
            Assert.AreEqual("João Gonçalves Müller", resultado.ListaSucesso[0].Nome);
            Assert.AreEqual("Coordenação", resultado.ListaSucesso[0].TipoParticipacao);
            Assert.AreEqual("Conceição Esperança", resultado.ListaSucesso[1].Nome);
            Assert.AreEqual("Apresentação", resultado.ListaSucesso[1].TipoParticipacao);
        }

        [TestMethod]
        public void ParseCsv_CodificadoEmIso88591_DevePreservarCaracteresAcentuados()
        {
            var isoEncoding = Encoding.GetEncoding("iso-8859-1");
            var csv = "CPF;Nome Completo;E-mail;Tipo de Participação\n" +
                      "040.268.930-57;João Gonçalves Müller;joao@ufs.br;Coordenação\n" +
                      "917.091.250-55;Érica Conceição;erica@ufs.br;Ouvinte";

            var bytesIso = isoEncoding.GetBytes(csv);
            using var ms = new MemoryStream(bytesIso);

            var resultado = _parser.Parse(ms, "inscritos_iso.csv");

            Assert.AreEqual(2, resultado.ListaSucesso.Count);
            Assert.AreEqual("João Gonçalves Müller", resultado.ListaSucesso[0].Nome);
            Assert.AreEqual("Coordenação", resultado.ListaSucesso[0].TipoParticipacao);
            Assert.AreEqual("Érica Conceição", resultado.ListaSucesso[1].Nome);
        }

        #endregion

        #region Testes de Validação Sintática e ListaErros

        [TestMethod]
        public void ParseCsv_LinhaComCpfInvalido_DeveAdicionarEmListaErros()
        {
            var csv = @"CPF;Nome Completo;E-mail
040.268.930-57;Valido Silva;valido@teste.com
111.111.111-11;Invalido Digitos Repetidos;invalido@teste.com
123.456.789-00;Invalido Digito Verificador;invalido2@teste.com";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(1, resultado.ListaSucesso.Count);
            Assert.AreEqual(2, resultado.ListaErros.Count);

            var erro1 = resultado.ListaErros[0];
            Assert.AreEqual(3, erro1.NumeroLinha);
            Assert.AreEqual("111.111.111-11", erro1.ValorBruto);
            Assert.AreEqual("CPF inválido", erro1.MotivoRecusa);

            var erro2 = resultado.ListaErros[1];
            Assert.AreEqual(4, erro2.NumeroLinha);
            Assert.AreEqual("123.456.789-00", erro2.ValorBruto);
            Assert.AreEqual("CPF inválido", erro2.MotivoRecusa);
        }

        [TestMethod]
        public void ParseCsv_LinhaComCpfAusente_DeveAdicionarEmListaErros()
        {
            var csv = @"CPF;Nome Completo;E-mail
040.268.930-57;Valido Silva;valido@teste.com
;Sem Cpf;semcpf@teste.com";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(1, resultado.ListaSucesso.Count);
            Assert.AreEqual(1, resultado.ListaErros.Count);

            var erro = resultado.ListaErros[0];
            Assert.AreEqual(3, erro.NumeroLinha);
            Assert.AreEqual("CPF ausente", erro.MotivoRecusa);
        }

        [TestMethod]
        public void ParseCsv_LinhaComNomeAusente_DeveAdicionarEmListaErros()
        {
            var csv = @"CPF;Nome Completo;E-mail
040.268.930-57;;valido@teste.com";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(0, resultado.ListaSucesso.Count);
            Assert.AreEqual(1, resultado.ListaErros.Count);

            var erro = resultado.ListaErros[0];
            Assert.AreEqual(2, erro.NumeroLinha);
            Assert.AreEqual("Nome ausente", erro.MotivoRecusa);
        }

        [TestMethod]
        public void ParseCsv_SemCabecalhoObrigatorio_DeveRetornarErroDescritivoSemExcecao()
        {
            var csv = @"Codigo;Descricao;Valor
1;Curso de Extensão A;100.00
2;Curso de Extensão B;200.00";

            var resultado = _parser.ParseCsv(csv);

            Assert.AreEqual(0, resultado.ListaSucesso.Count);
            Assert.AreEqual(1, resultado.ListaErros.Count);
            Assert.IsTrue(resultado.ListaErros[0].MotivoRecusa.Contains("Cabeçalho com colunas obrigatórias"));
        }

        [TestMethod]
        public void Parse_ArquivoVazioOuNulo_DeveRetornarErroSemLancarExcecao()
        {
            var resNull = _parser.Parse((Stream)null!);
            Assert.AreEqual(0, resNull.ListaSucesso.Count);
            Assert.AreEqual(1, resNull.ListaErros.Count);

            using var msVazio = new MemoryStream();
            var resVazio = _parser.Parse(msVazio);
            Assert.AreEqual(0, resVazio.ListaSucesso.Count);
            Assert.AreEqual(1, resVazio.ListaErros.Count);
        }

        [TestMethod]
        public void Parse_ArquivoCorrompidoOuFormatoInesperado_NaoLancaExcecao()
        {
            var bytesLixo = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using var ms = new MemoryStream(bytesLixo);

            var resultado = _parser.Parse(ms, "corrompido.xlsx");

            Assert.IsNotNull(resultado);
            Assert.AreEqual(0, resultado.ListaSucesso.Count);
            Assert.IsTrue(resultado.ListaErros.Count > 0);
        }

        #endregion

        #region Testes de Parsing Excel XLSX

        [TestMethod]
        public void ParseExcel_XlsxValidoComMetadados_DeveExtrairSucessosEErros()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Inscritos SIGAA");

            // Metadados do SIGAA
            ws.Cell(1, 1).Value = "UNIVERSIDADE FEDERAL DE SERGIPE";
            ws.Cell(2, 1).Value = "RELATÓRIO DE INSCRITOS NO EVENTO";
            ws.Cell(3, 1).Value = "Emitido em 06/11/2026";

            // Cabeçalho na linha 5
            ws.Cell(5, 1).Value = "CPF";
            ws.Cell(5, 2).Value = "Nome Completo";
            ws.Cell(5, 3).Value = "E-mail";
            ws.Cell(5, 4).Value = "Tipo de Participação";

            // Linha 6: Válido 1
            ws.Cell(6, 1).Value = "040.268.930-57";
            ws.Cell(6, 2).Value = "Carlos Drummond";
            ws.Cell(6, 3).Value = "carlos@drummond.com";
            ws.Cell(6, 4).Value = "Palestrante";

            // Linha 7: Válido 2 (CPF numérico não formatado com zero inicial)
            ws.Cell(7, 1).Value = "91709125055";
            ws.Cell(7, 2).Value = "Clarice Lispector";
            ws.Cell(7, 3).Value = "clarice@lispector.com";
            ws.Cell(7, 4).Value = "Ouvinte";

            // Linha 8: Linha vazia intencional

            // Linha 9: Inválido (CPF falso)
            ws.Cell(9, 1).Value = "111.111.111-11";
            ws.Cell(9, 2).Value = "Machado de Assis";
            ws.Cell(9, 3).Value = "machado@assis.com";
            ws.Cell(9, 4).Value = "Autor";

            // Linha 10: Inválido (Nome ausente)
            ws.Cell(10, 1).Value = "206.015.300-04";
            ws.Cell(10, 2).Value = "";
            ws.Cell(10, 3).Value = "semnome@email.com";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var resultado = _parser.Parse(stream, "relatorio_sigaa.xlsx");

            Assert.IsNotNull(resultado);
            Assert.AreEqual(2, resultado.ListaSucesso.Count);
            Assert.AreEqual(2, resultado.ListaErros.Count);

            // Registro 1
            Assert.AreEqual("04026893057", resultado.ListaSucesso[0].Cpf);
            Assert.AreEqual("Carlos Drummond", resultado.ListaSucesso[0].Nome);
            Assert.AreEqual("Palestrante", resultado.ListaSucesso[0].TipoParticipacao);

            // Registro 2
            Assert.AreEqual("91709125055", resultado.ListaSucesso[1].Cpf);
            Assert.AreEqual("Clarice Lispector", resultado.ListaSucesso[1].Nome);

            // Erro 1: Linha 9 (CPF inválido)
            Assert.AreEqual(9, resultado.ListaErros[0].NumeroLinha);
            Assert.AreEqual("CPF inválido", resultado.ListaErros[0].MotivoRecusa);

            // Erro 2: Linha 10 (Nome ausente)
            Assert.AreEqual(10, resultado.ListaErros[1].NumeroLinha);
            Assert.AreEqual("Nome ausente", resultado.ListaErros[1].MotivoRecusa);
        }

        #endregion

        #region Testes de Arquitetura e Aliases

        [TestMethod]
        public void Arquitetura_ClassesDentroDePessoaDTO_DevemSerInstanciaveis()
        {
            // Valida criação de instâncias do DTO PessoaInscritaDto
            var dto = new PessoaInscritaDto
            {
                Cpf = "04026893057",
                Nome = "Teste Arquitetura",
                Email = "teste@arquitetura.com",
                TipoParticipação = "Participante"
            };

            Assert.AreEqual("04026893057", dto.Cpf);
            Assert.AreEqual("Participante", dto.TipoParticipacao);
            Assert.AreEqual("Participante", dto.TipoParticipação);

            var resultado = new ResultadoParsingSigaaDto();
            resultado.ListaSucesso.Add(dto);
            Assert.AreEqual(1, resultado.ListaSucesso.Count);
            Assert.IsTrue(resultado.Sucesso);
        }

        [TestMethod]
        public void Aliases_PlanilhaParser_DeveSuportarMinusculoESigaa()
        {
            var p1 = new planilhaParser();
            var p2 = new SigaaPlanilhaParser();
            var p3 = new EventoWeb.Helper.planilhaParser();

            Assert.IsNotNull(p1);
            Assert.IsNotNull(p2);
            Assert.IsNotNull(p3);

            var csv = "CPF;Nome\n040.268.930-57;Nome Teste";
            var r1 = p1.ParseCsv(csv);
            var r2 = p2.ParseCsv(csv);
            var r3 = p3.ParseCsv(csv);

            Assert.AreEqual(1, r1.ListaSucesso.Count);
            Assert.AreEqual(1, r2.ListaSucesso.Count);
            Assert.AreEqual(1, r3.ListaSucesso.Count);
        }

        #endregion
    }
}

