using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Tests
{
    /// <summary>
    /// Cobertura do caso de uso Consultar Dashboard Evento (Issue #727).
    /// Aba 1: Visão Geral &amp; Finanças. Aba 2: Operação &amp; Presença.
    /// </summary>
    [TestClass()]
    public class DashboardEventoServiceTests
    {
        private EventoContext _context = null!;
        private IEventoService _eventoService = null!;

        [TestInitialize]
        public void Initialize()
        {
            var builder = new DbContextOptionsBuilder<EventoContext>();
            builder.UseInMemoryDatabase("DashboardEvento");
            _context = new EventoContext(builder.Options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            _context.Eventos.Add(new Evento
            {
                Id = 1,
                Nome = "SEMINFO",
                Descricao = "Evento teste dashboard",
                Status = "A",
                DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                DataInicioInscricao = new DateTime(2024, 9, 2, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 9, 30, 23, 59, 0),
                FrequenciaMinimaCertificado = 75,
                VagasOfertadas = 200,
                VagasReservadas = 0,
                VagasDisponiveis = 196,
                CargaHoraria = 20,
            });

            _context.Tipoinscricaos.AddRange(
                new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Gratuita", Descricao = "Isento", Valor = 0, DataInicio = new DateTime(2024, 9, 2), Datafim = new DateTime(2024, 9, 30) },
                new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "Meia", Descricao = "Meia-entrada", Valor = 40, DataInicio = new DateTime(2024, 9, 2), Datafim = new DateTime(2024, 9, 30) },
                new Tipoinscricao { Id = 3, IdEvento = 1, Nome = "Inteira", Descricao = "Valor cheio", Valor = 80, DataInicio = new DateTime(2024, 9, 2), Datafim = new DateTime(2024, 9, 30) });

            _context.Subeventos.AddRange(
                new Subevento { Id = 10, IdEvento = 1, Nome = "Minicurso Python", Descricao = "Minicurso", DataInicio = new DateTime(2024, 10, 2), DataFim = new DateTime(2024, 10, 3), Status = "A", DataInicioInscricao = new DateTime(2024, 9, 2), DataFimInscricao = new DateTime(2024, 9, 30), VagasOfertadas = 50, IdTipoEvento = 1 },
                new Subevento { Id = 11, IdEvento = 1, Nome = "Palestra IA", Descricao = "Palestra", DataInicio = new DateTime(2024, 10, 4), DataFim = new DateTime(2024, 10, 4), Status = "A", DataInicioInscricao = new DateTime(2024, 9, 2), DataFimInscricao = new DateTime(2024, 9, 30), VagasOfertadas = 100, IdTipoEvento = 1 });

            _context.Pessoas.AddRange(
                new Pessoa { Id = 1, Nome = "Ana", NomeCracha = "Ana", Cpf = "11111111111", Email = "ana@e.com", Sexo = "F", Estado = "SP" },
                new Pessoa { Id = 2, Nome = "Bia", NomeCracha = "Bia", Cpf = "22222222222", Email = "bia@e.com", Sexo = "F", Estado = "MG" },
                new Pessoa { Id = 3, Nome = "Caio", NomeCracha = "Caio", Cpf = "33333333333", Email = "caio@e.com", Sexo = "M", Estado = "BA" },
                new Pessoa { Id = 4, Nome = "Dan", NomeCracha = "Dan", Cpf = "44444444444", Email = "dan@e.com", Sexo = "M", Estado = null });

            // 4 participantes: 2 ativos, 1 solicitado, 1 cancelado
            _context.Inscricaopessoaeventos.AddRange(
                new Inscricaopessoaevento { Id = 101, IdPessoa = 1, IdEvento = 1, IdPapel = 4, IdTipoInscricao = 1, DataInscricao = new DateTime(2024, 9, 5, 10, 0, 0), Status = "A", ValorTotal = 0, FrequenciaFinal = 80 },
                new Inscricaopessoaevento { Id = 102, IdPessoa = 2, IdEvento = 1, IdPapel = 4, IdTipoInscricao = 2, DataInscricao = new DateTime(2024, 9, 6, 10, 0, 0), Status = "A", ValorTotal = 40, FrequenciaFinal = 90 },
                new Inscricaopessoaevento { Id = 103, IdPessoa = 3, IdEvento = 1, IdPapel = 4, IdTipoInscricao = 3, DataInscricao = new DateTime(2024, 9, 7, 10, 0, 0), Status = "S", ValorTotal = 80, FrequenciaFinal = 0 },
                new Inscricaopessoaevento { Id = 104, IdPessoa = 4, IdEvento = 1, IdPapel = 4, IdTipoInscricao = 1, DataInscricao = new DateTime(2024, 9, 8, 10, 0, 0), Status = "C", ValorTotal = 0, FrequenciaFinal = 10 });

            _context.Pagamentos.AddRange(
                new Pagamento { Id = 201, IdInscricaoPessoaEvento = 102, Valor = 40, Status = "C", Forma = "C", DataPagamento = new DateTime(2024, 9, 6), DataSolicitacao = new DateTime(2024, 9, 6) },
                new Pagamento { Id = 202, IdInscricaoPessoaEvento = 103, Valor = 80, Status = "S", Forma = "B", DataPagamento = new DateTime(2024, 9, 7), DataSolicitacao = new DateTime(2024, 9, 7) });

            // Check-in de Ana e Bia no evento
            _context.Participacaopessoaeventos.AddRange(
                new Participacaopessoaevento { Id = 301, IdPessoa = 1, IdEvento = 1, Entrada = new DateTime(2024, 10, 2, 8, 0, 0) },
                new Participacaopessoaevento { Id = 302, IdPessoa = 2, IdEvento = 1, Entrada = new DateTime(2024, 10, 2, 8, 5, 0) });

            // Ocupação: sub 10 com Ana+Bia, sub 11 com Caio
            _context.Inscricaopessoasubeventos.AddRange(
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, IdPapel = 4, IdTipoInscricao = 1, DataInscricao = new DateTime(2024, 9, 5), Status = "A", Valor = 0 },
                new Inscricaopessoasubevento { IdPessoa = 2, IdSubEvento = 10, IdPapel = 4, IdTipoInscricao = 2, DataInscricao = new DateTime(2024, 9, 6), Status = "A", Valor = 40 },
                new Inscricaopessoasubevento { IdPessoa = 3, IdSubEvento = 11, IdPapel = 4, IdTipoInscricao = 3, DataInscricao = new DateTime(2024, 9, 7), Status = "S", Valor = 80 });

            _context.Participacaopessoasubeventos.Add(
                new Participacaopessoasubevento { Id = 401, IdPessoa = 1, IdSubEvento = 10, Entrada = new DateTime(2024, 10, 2, 8, 0, 0) });

            _context.SaveChanges();
            _context.ChangeTracker.Clear();

            _eventoService = new EventoService(_context);
        }

        [TestMethod()]
        public void GetDashboardFinanceiro_TotaisFunilReceita()
        {
            var fin = _eventoService.GetDashboardFinanceiro(1, new DashboardFiltrosDTO());

            Assert.AreEqual(4, fin.TotalInscritos);
            Assert.AreEqual(200, fin.VagasOfertadas);
            Assert.AreEqual(2m, fin.PercentualOcupacao);

            Assert.AreEqual(2, fin.Funil.Ativas);
            Assert.AreEqual(1, fin.Funil.Solicitadas);
            Assert.AreEqual(1, fin.Funil.Canceladas);

            Assert.AreEqual(40m, fin.ReceitaConfirmada);
            Assert.AreEqual(80m, fin.ValorEmAberto);
            Assert.AreEqual(1, fin.PagamentosEmAberto);
            Assert.AreEqual(10m, fin.TicketMedio);

            Assert.AreEqual(1, fin.PorForma.Count);
            Assert.AreEqual("C", fin.PorForma[0].Forma);
            Assert.AreEqual(100m, fin.PorForma[0].Percentual);

            Assert.AreEqual(3, fin.PorCategoria.Count);
            Assert.AreEqual(2, fin.PorCategoria.First(c => c.Nome == "Gratuita").Quantidade);
        }

        [TestMethod()]
        public void GetDashboardFinanceiro_FiltroTipo()
        {
            var fin = _eventoService.GetDashboardFinanceiro(1, new DashboardFiltrosDTO { IdTipoInscricao = 2 });

            Assert.AreEqual(1, fin.TotalInscritos);
            Assert.AreEqual(40m, fin.ReceitaConfirmada);
        }

        [TestMethod()]
        [ExpectedException(typeof(ServiceException))]
        public void GetDashboardFinanceiro_EventoInexistente()
        {
            _eventoService.GetDashboardFinanceiro(999, new DashboardFiltrosDTO());
        }

        [TestMethod()]
        public void GetDashboardOperacao_PresencaCertificadoDemografia()
        {
            var op = _eventoService.GetDashboardOperacao(1, new DashboardFiltrosDTO());

            Assert.AreEqual(2, op.Presentes);
            Assert.AreEqual(2, op.Ausentes);
            Assert.AreEqual(2, op.AptosCertificado);
            Assert.AreEqual(85m, op.FrequenciaMedia);

            Assert.AreEqual(2, op.OcupacaoSubeventos.Count);
            Assert.AreEqual(2, op.OcupacaoSubeventos.First(s => s.IdSubevento == 10).Ocupados);

            Assert.AreEqual(2, op.PorSexo.Feminino);
            Assert.AreEqual(2, op.PorSexo.Masculino);
            Assert.IsTrue(op.PorOrigem.Any(o => o.Estado == "SP"));
        }

        [TestMethod()]
        public void GetDashboardOperacao_FiltroSubevento()
        {
            var op = _eventoService.GetDashboardOperacao(1, new DashboardFiltrosDTO { IdSubevento = 10 });

            Assert.AreEqual(1, op.OcupacaoSubeventos.Count);
            Assert.AreEqual(1, op.Presentes);
        }

        [TestMethod()]
        [ExpectedException(typeof(ServiceException))]
        public void GetDashboardOperacao_EventoInexistente()
        {
            _eventoService.GetDashboardOperacao(999, new DashboardFiltrosDTO());
        }
    }
}
