using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Tests
{
    [TestClass()]
    public class ParticipacaopessoaeventoServiceTests
    {
        private EventoContext _context;
        private IParticipacaoPessoaEventoService _participacaoService;

        [TestInitialize]
        public void Initialize()
        {
            var builder = new DbContextOptionsBuilder<EventoContext>();
            builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            var options = builder.Options;

            _context = new EventoContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var pessoas = new List<Pessoa>
            {
                new Pessoa
                {
                    Id = 1,
                    Nome = "João Vitor Sodré",
                    NomeCracha = "Sodré",
                    Cpf = "12246232367",
                    Email = "email@gmail.com",
                },
                new Pessoa
                {
                    Id = 2,
                    Nome = "Nagibe Santos Wanus Junior",
                    NomeCracha = "Nagibe Junior",
                    Cpf = "12343455678",
                    Email = "nagibejr@gmail.com",
                },
            };

            var participacoes = new List<Participacaopessoaevento>
            {
                new Participacaopessoaevento
                {
                    Id = 1,
                    IdPessoa = 1,
                    IdEvento = 1,
                    Entrada = new DateTime(2024, 10, 2, 8, 0, 0),
                },
                new Participacaopessoaevento
                {
                    Id = 2,
                    IdPessoa = 2,
                    IdEvento = 2,
                    Entrada = new DateTime(2024, 10, 2, 9, 0, 0),
                },
            };

            _context.AddRange(pessoas);
            _context.AddRange(participacoes);
            _context.SaveChanges();

            _participacaoService = new ParticipacaoPessoaEventoService(_context);
        }

        // Garante que a navegação IdPessoaNavigation vem carregada,
        // evitando a NullReferenceException na tela de participação.
        [TestMethod()]
        public async Task GetAllAsync_CarregaIdPessoaNavigation()
        {
            var lista = await _participacaoService.GetAllAsync();

            Assert.IsNotNull(lista);
            Assert.AreEqual(2, lista.Count);
            Assert.IsTrue(lista.All(p => p.IdPessoaNavigation != null));

            var primeira = lista.First(p => p.Id == 1);
            Assert.AreEqual("João Vitor Sodré", primeira.IdPessoaNavigation.Nome);
        }

        // Verifica que, ao filtrar por evento (como faz o controller Index),
        // apenas as participações do evento informado são retornadas.
        [TestMethod()]
        public async Task GetAllAsync_FiltraPorEvento()
        {
            var lista = await _participacaoService.GetAllAsync();
            var doEvento1 = lista.Where(f => f.IdEvento == 1).ToList();

            Assert.AreEqual(1, doEvento1.Count);
            Assert.AreEqual((uint)1, doEvento1[0].IdPessoa);
        }

        [TestMethod()]
        public async Task GetByEventoAsync_RetornaApenasDoEventoInformado()
        {
            var lista = await _participacaoService.GetByEventoAsync(1);

            Assert.IsNotNull(lista);
            Assert.AreEqual(1, lista.Count);
            Assert.AreEqual((uint)1, lista[0].IdPessoa);
            Assert.IsNotNull(lista[0].IdPessoaNavigation);
            Assert.AreEqual("João Vitor Sodré", lista[0].IdPessoaNavigation.Nome);
        }

        [TestMethod()]
        public async Task AlterarOuExcluirPresencaFinalizada_RecalculaEventoESubevento()
        {
            var inicio = new DateTime(2024, 10, 2, 8, 0, 0);
            _context.Eventos.Add(new Evento
            {
                Id = 1, Nome = "Evento Finalizado", Descricao = "Desc", Status = "F",
                DataInicio = inicio, DataFim = inicio.AddHours(4), CargaHoraria = 4, IdTipoEvento = 1
            });
            _context.Subeventos.Add(new Subevento
            {
                Id = 1, IdEvento = 1, Nome = "Sub", Descricao = "Sub", Status = "F",
                DataInicio = inicio, DataFim = inicio.AddHours(2),
                DataInicioInscricao = inicio.AddDays(-2), DataFimInscricao = inicio.AddDays(-1),
                CargaHoraria = 2, IdTipoEvento = 1
            });
            _context.Inscricaopessoaeventos.Add(new Inscricaopessoaevento
            {
                Id = 1, IdEvento = 1, IdPessoa = 1, IdPapel = 4, Status = "A", DataInscricao = inicio
            });
            _context.Inscricaopessoasubeventos.Add(new Inscricaopessoasubevento
            {
                IdPessoa = 1, IdSubEvento = 1, IdPapel = 4, Status = "A", DataInscricao = inicio
            });
            _context.Participacaopessoasubeventos.Add(new Participacaopessoasubevento
            {
                Id = 1, IdPessoa = 1, IdSubEvento = 1, Entrada = inicio, Saida = inicio.AddHours(1)
            });
            _context.SaveChanges();

            var presencaEvento = await _participacaoService.GetByIdAsync(1);
            presencaEvento.Saida = inicio.AddHours(2);
            Assert.IsTrue(await _participacaoService.UpdateAsync(presencaEvento));
            Assert.AreEqual(50m, _context.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);

            Assert.IsTrue(await _participacaoService.DeleteAsync(1));
            Assert.AreEqual(0m, _context.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);

            var serviceSub = new ParticipacaoPessoaSubEventoService(_context);
            var presencaSub = _context.Participacaopessoasubeventos.Find((uint)1)!;
            presencaSub.Saida = inicio.AddHours(2);
            serviceSub.Update(presencaSub);
            Assert.AreEqual(100m, _context.Inscricaopessoasubeventos.Find((uint)1, (uint)1)!.FrequenciaFinal);

            serviceSub.Delete(1);
            Assert.AreEqual(0m, _context.Inscricaopessoasubeventos.Find((uint)1, (uint)1)!.FrequenciaFinal);
        }
    }
}
