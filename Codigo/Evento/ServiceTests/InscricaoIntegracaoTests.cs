using Core;
using Core.Service;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Tests
{
    /// <summary>
    /// Testes de integração com provedor relacional (SQLite com FKs ativas).
    /// Cobrem a atomicidade dos casos de uso críticos (#841/#861), que não é
    /// exercitada pelos testes InMemory.
    /// </summary>
    [TestClass()]
    public class InscricaoIntegracaoTests
    {
        private SqliteConnection _connection = null!;
        private EventoContext _context = null!;
        private EventoService _eventoService = null!;
        private SubeventoService _subeventoService = null!;
        private InscricaoService _inscricaoService = null!;
        private InscricaopessoaeventoService _inscricaopessoaeventoService = null!;

        [TestInitialize]
        public void Initialize()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<EventoContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new EventoContext(options);
            SqliteSchemaEvento.Criar(_connection);

            _eventoService = new EventoService(_context);
            _subeventoService = new SubeventoService(_context);
            _inscricaoService = new InscricaoService(_context, new MockUserManager<UsuarioIdentity>(), _eventoService, _subeventoService);
            _inscricaopessoaeventoService = new InscricaopessoaeventoService(_context, _eventoService, _subeventoService);

            SemearBase();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private void SemearBase()
        {
            _context.Estadosbrasils.AddRange(
                new Estadosbrasil { Estado = "SE", Nome = "Sergipe" },
                new Estadosbrasil { Estado = "SP", Nome = "São Paulo" },
                new Estadosbrasil { Estado = "MG", Nome = "Minas Gerais" });

            _context.Papels.AddRange(
                new Papel { Id = 1, Nome = "Administrador" },
                new Papel { Id = 2, Nome = "Gestor" },
                new Papel { Id = 3, Nome = "Colaborador" },
                new Papel { Id = 4, Nome = "Participante" });

            _context.Tipoeventos.AddRange(
                new Tipoevento { Id = 1, Nome = "Minicurso" },
                new Tipoevento { Id = 2, Nome = "Palestra" },
                new Tipoevento { Id = 3, Nome = "Festival" });

            _context.Eventos.Add(new Evento
            {
                Id = 1,
                Nome = "SEMINFO",
                Descricao = "Evento de teste",
                Status = "A",
                DataInicio = new DateTime(2024, 10, 2),
                DataFim = new DateTime(2024, 10, 7),
                DataInicioInscricao = new DateTime(2024, 9, 2),
                DataFimInscricao = new DateTime(2024, 9, 30),
                FrequenciaMinimaCertificado = 75,
                VagasOfertadas = 200,
                VagasReservadas = 0,
                VagasDisponiveis = 200,
            });

            _context.Tipoinscricaos.Add(new Tipoinscricao
            {
                Id = 1,
                IdEvento = 1,
                Nome = "Inteira",
                Descricao = "Valor cheio",
                Valor = 80,
                DataInicio = new DateTime(2024, 9, 2),
                Datafim = new DateTime(2024, 9, 30)
            });

            _context.Subeventos.Add(new Subevento
            {
                Id = 10,
                IdEvento = 1,
                Nome = "Minicurso",
                Descricao = "Minicurso",
                DataInicio = new DateTime(2024, 10, 2),
                DataFim = new DateTime(2024, 10, 3),
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 9, 2),
                DataFimInscricao = new DateTime(2024, 9, 30),
                VagasOfertadas = 50,
                IdTipoEvento = 1
            });

            _context.Pessoas.AddRange(
                new Pessoa { Id = 1, Nome = "Ana", NomeCracha = "Ana", Cpf = "11111111111", Email = "ana@e.com", Sexo = "F", Estado = "SP" },
                new Pessoa { Id = 2, Nome = "Bia", NomeCracha = "Bia", Cpf = "22222222222", Email = "bia@e.com", Sexo = "M", Estado = "SE" });

            _context.Inscricaopessoaeventos.Add(new Inscricaopessoaevento
            {
                Id = 101,
                IdPessoa = 1,
                IdEvento = 1,
                IdPapel = 4,
                IdTipoInscricao = 1,
                DataInscricao = new DateTime(2024, 9, 5),
                Status = "A",
                ValorTotal = 80,
                FrequenciaFinal = 90
            });

            _context.Inscricaopessoasubeventos.Add(new Inscricaopessoasubevento
            {
                IdPessoa = 1,
                IdSubEvento = 10,
                IdPapel = 4,
                IdTipoInscricao = 1,
                DataInscricao = new DateTime(2024, 9, 5),
                Status = "A",
                Valor = 80
            });

            _context.Pagamentos.Add(new Pagamento
            {
                Id = 201,
                IdInscricaoPessoaEvento = 101,
                Valor = 80,
                Status = "C",
                Forma = "C",
                DataPagamento = new DateTime(2024, 9, 5),
                DataSolicitacao = new DateTime(2024, 9, 5)
            });

            _context.SaveChanges();
            _context.ChangeTracker.Clear();
        }

        [TestMethod()]
        public void ModeloEvento_UsaDeleteRestrictNasFkCriticas()
        {
            // Garante que o schema SQLite usado nos testes deste arquivo continua
            // coerente com o modelo EF: as FKs críticas são RESTRICT.
            var fksCriticas = new (string Dependente, string Principal)[]
            {
                ("Inscricaopessoasubevento", "Subevento"),
                ("Participacaopessoasubevento", "Subevento"),
                ("Pagamento", "Inscricaopessoaevento"),
                ("Inscricaopessoaevento", "Evento"),
                ("Inscricaopessoaevento", "Tipoinscricao")
            };

            foreach (var (dependente, principal) in fksCriticas)
            {
                var fk = _context.Model
                    .GetEntityTypes()
                    .Single(e => e.ClrType.Name == dependente)
                    .GetForeignKeys()
                    .Single(f => f.PrincipalEntityType.ClrType.Name == principal);

                Assert.AreEqual(
                    Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict,
                    fk.DeleteBehavior,
                    $"FK {dependente} -> {principal} deve ser Restrict");
            }
        }

        [TestMethod()]
        public async Task EventoServiceDelete_ComInscricaoEmSubevento_ConcluiELimpaDependentes()
        {
            Evento evento = _eventoService.Get(1);
            evento.Status = "I";
            _eventoService.Edit(evento,null);
            _inscricaoService.CancelarInscricaoParticipante(1, 1);
            _eventoService.Delete(1);

            _context.ChangeTracker.Clear();

            Assert.AreEqual(0, _context.Eventos.Count());
            Assert.AreEqual(0, _context.Subeventos.Count());
            Assert.AreEqual(0, _context.Inscricaopessoaeventos.Count());
            Assert.AreEqual(0, _context.Inscricaopessoasubeventos.Count());
            Assert.AreEqual(0, _context.Pagamentos.Count());
            Assert.AreEqual(0, _context.Tipoinscricaos.Count());
        }

        [TestMethod()]
        public void EventoServiceDelete_FalhaInesperadaNoBanco_NaoApagaNada()
        {
            // Sabotagem no banco: um trigger que falha na remoção do subevento,
            // simulando uma falha inesperada que o código não consegue antecipar.
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TRIGGER falha_delete_subevento BEFORE DELETE ON subevento
                BEGIN SELECT RAISE(ABORT, 'falha simulada no banco'); END;";
                command.ExecuteNonQuery();
            }

            Assert.ThrowsException<ServiceException>(() => _eventoService.Delete(1));

            _context.ChangeTracker.Clear();
            Assert.AreEqual(1, _context.Eventos.Count());
            Assert.AreEqual(1, _context.Subeventos.Count());
            Assert.AreEqual(1, _context.Inscricaopessoaeventos.Count());
            Assert.AreEqual(1, _context.Inscricaopessoasubeventos.Count());
            Assert.AreEqual(1, _context.Pagamentos.Count());
        }

        [TestMethod()]
        public void EventoServiceDelete_ComJuncaoTipoInscricaoSubevento_Conclui()
        {
            Evento evento = _eventoService.Get(1);
            evento.Status = "I";
            _eventoService.Edit(evento, null);
            var tipo = _context.Tipoinscricaos.First();
            var subevento = _context.Subeventos.First();
            tipo.IdSubEventos.Add(subevento);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            _inscricaoService.CancelarInscricaoParticipante(1, 1);
            _eventoService.Delete(1);

            _context.ChangeTracker.Clear();
            Assert.AreEqual(0, _context.Eventos.Count());
            Assert.AreEqual(0, _context.Subeventos.Count());
            Assert.AreEqual(0, _context.Tipoinscricaos.Count());
        }

        [TestMethod()]
        public void CancelarInscricaoParticipante_Sucesso_RemoveInscricaoELiberaVaga()
        {
            var removidas = _inscricaoService.CancelarInscricaoParticipante(1, 1);

            Assert.AreEqual(1, removidas);

            _context.ChangeTracker.Clear();
            Assert.AreEqual(0, _context.Inscricaopessoaeventos.Count());
            Assert.AreEqual(0, _context.Inscricaopessoasubeventos.Count());
            Assert.AreEqual(0, _context.Pagamentos.Count());
            Assert.AreEqual(200, _context.Eventos.First().VagasDisponiveis);
            Assert.AreEqual(50u, _context.Subeventos.First().VagasDisponiveis);
        }

        [TestMethod()]
        public void InscricaoLote_ComInscricoesPendentes_AtualizaVagasDoEventoESubevento()
        {
            var vagasOfertadasEvento = _context.Eventos.First().VagasOfertadas;
            var vagasOfertadasSubevento = _context.Subeventos.First().VagasOfertadas;

            _inscricaoService.CreateInscricoesEmLote(
                1,
                new List<Inscricaopessoaevento>
                {
                    new Inscricaopessoaevento
                    {
                        IdPessoa = 2,
                        IdEvento = 1,
                        IdPapel = 4,
                        IdTipoInscricao = 1,
                        DataInscricao = DateTime.Now,
                        Status = "S",
                        ValorTotal = 80
                    }
                },
                new List<Inscricaopessoasubevento>
                {
                    new Inscricaopessoasubevento
                    {
                        IdPessoa = 2,
                        IdSubEvento = 10,
                        IdPapel = 4,
                        IdTipoInscricao = 1,
                        DataInscricao = DateTime.Now,
                        Status = "S",
                        Valor = 80
                    }
                },
                new List<uint> { 10 });

            _context.ChangeTracker.Clear();

            // 1 participante já inscrito + 1 nova inscrição = 2 participantes.
            Assert.AreEqual(vagasOfertadasEvento - 2, _context.Eventos.First().VagasDisponiveis);
            Assert.AreEqual((uint)(vagasOfertadasSubevento - 2), _context.Subeventos.First().VagasDisponiveis);
        }

        [TestMethod()]
        public void CreateInscricoesEmLote_FalhaNoSaveChanges_NaoGravaLote()
        {
            var totalAntes = _context.Inscricaopessoaeventos.Count();

            // Duplicidade proposital: força a falha do único SaveChanges do caso de uso.
            _context.Inscricaopessoaeventos.Add(new Inscricaopessoaevento
            {
                Id = 101,
                IdPessoa = 2,
                IdEvento = 1,
                IdPapel = 4,
                DataInscricao = DateTime.Now,
                Status = "A"
            });

            Assert.ThrowsException<DbUpdateException>(() =>
                _inscricaoService.CreateInscricoesEmLote(
                    1,
                    new List<Inscricaopessoaevento>
                    {
                        new Inscricaopessoaevento
                        {
                            IdPessoa = 2,
                            IdEvento = 1,
                            IdPapel = 4,
                            DataInscricao = DateTime.Now,
                            Status = "A"
                        }
                    },
                    new List<Inscricaopessoasubevento>(),
                    new List<uint>()));

            _context.ChangeTracker.Clear();
            Assert.AreEqual(totalAntes, _context.Inscricaopessoaeventos.Count());
        }

        [TestMethod()]
        public void InscricaopessoaeventoServiceDelete_RecalculaVagasDoEvento()
        {
            _context.Inscricaopessoaeventos.Add(new Inscricaopessoaevento
            {
                Id = 102,
                IdPessoa = 2,
                IdEvento = 1,
                IdPapel = 4,
                IdTipoInscricao = 1,
                DataInscricao = DateTime.Now,
                Status = "A",
                ValorTotal = 80
            });
            _context.SaveChanges();

            _eventoService.AtualizarVagasDisponiveis(1);
            _context.ChangeTracker.Clear();

            Assert.AreEqual(198, _context.Eventos.First().VagasDisponiveis);

            _inscricaopessoaeventoService.Delete(102);

            _context.ChangeTracker.Clear();
            Assert.AreEqual(0, _context.Inscricaopessoaeventos.Count(i => i.Id == 102));
            Assert.AreEqual(199, _context.Eventos.First().VagasDisponiveis);
        }

        [TestMethod()]
        public void InscricaopessoaeventoServiceDelete_ComPagamentoESubevento_RemoveRegistrosERecalculaVagas()
        {
            _eventoService.AtualizarVagasDisponiveis(1);
            _subeventoService.AtualizarVagasDisponiveis(10);
            _context.ChangeTracker.Clear();

            Assert.AreEqual(199, _context.Eventos.First().VagasDisponiveis);
            Assert.AreEqual(49u, _context.Subeventos.First().VagasDisponiveis);
            Assert.AreEqual(1, _context.Pagamentos.Count(p => p.IdInscricaoPessoaEvento == 101));
            Assert.AreEqual(1, _context.Inscricaopessoasubeventos.Count(s => s.IdPessoa == 1 && s.IdSubEvento == 10));

            _inscricaopessoaeventoService.Delete(101);

            _context.ChangeTracker.Clear();
            Assert.AreEqual(0, _context.Inscricaopessoaeventos.Count(i => i.Id == 101));
            Assert.AreEqual(0, _context.Pagamentos.Count(p => p.IdInscricaoPessoaEvento == 101));
            Assert.AreEqual(0, _context.Inscricaopessoasubeventos.Count(s => s.IdPessoa == 1 && s.IdSubEvento == 10));
            Assert.AreEqual(200, _context.Eventos.First().VagasDisponiveis);
            Assert.AreEqual(50u, _context.Subeventos.First().VagasDisponiveis);
        }
    }
}
