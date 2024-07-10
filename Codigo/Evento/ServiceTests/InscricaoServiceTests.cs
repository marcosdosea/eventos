using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Service.Tests
{
    [TestClass()]
    public class InscricaoServiceTests
    {
        private EventoContext _context;
        private IInscricaoService _inscricaoService;

        [TestInitialize]
        public void Initialize()
        {
            //Arrange
            var builder = new DbContextOptionsBuilder<EventoContext>();
            builder.UseInMemoryDatabase("Evento");
            var options = builder.Options;

            _context = new EventoContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            var inscricoes = new List<Inscricaopessoaevento>
                {
                new Inscricaopessoaevento
                {
                        Id = 1,
                        IdPessoa = 1,
                        IdEvento = 1,
                        IdPapel = 1,
                        IdTipoInscricao = 1,
                        DataInscricao = new DateTime(2024, 07, 3, 0, 0, 0),
                        ValorTotal = 0,
                        Status = "A",
                        FrequenciaFinal = 1,
                    },
                new Inscricaopessoaevento
                {
                        Id = 2,
                        IdPessoa = 2,
                        IdEvento = 1,
                        IdPapel = 1,
                        IdTipoInscricao = 1,
                        DataInscricao = new DateTime(2024, 07, 2, 0, 0, 0),
                        ValorTotal = 0,
                        Status = "C",
                        FrequenciaFinal = 0,
                    },
                new Inscricaopessoaevento
                {
                        Id = 3,
                        IdPessoa = 3,
                        IdEvento = 1,
                        IdPapel = 1,
                        IdTipoInscricao = 1,
                        DataInscricao = new DateTime(2024, 07, 5, 0, 0, 0),
                        ValorTotal = 0,
                        Status = "S",
                        FrequenciaFinal = 0,
                    },
                };

            var eventos = new List<Evento>
                {
                new Evento
                {
                    Id = 1,
                    Nome = "SEMINFO",
                    Descricao = "Evento para a semana da tecnologia",
                    DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                    InscricaoGratuita = 1,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                    ValorInscricao = 0,
                    Website = "www.itatechjr.com.br",
                    EmailEvento = "DSI@academico.ufs.br",
                    EventoPublico = 1,
                    Cep = "49506036",
                    Estado = "SE",
                    Cidade = "Itabaiana",
                    Bairro = "Porto",
                    Rua = " Av. Vereador Olímpio Grande",
                    Numero = "s/n",
                    Complemento = "Universidade",
                    PossuiCertificado = 1,
                    FrequenciaMinimaCertificado = 1,
                    IdTipoEvento = 1,
                    VagasOfertadas = 100,
                    VagasReservadas = 35,
                    VagasDisponiveis = 65,
                    TempoMinutosReserva = 240,
                    CargaHoraria = 4,
                    },
                new Evento
                {
                        Id = 2,
                        Nome = "SEMAC",
                        Descricao = "Semana academica de cursos",
                        DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                        InscricaoGratuita = 1,
                        Status = "F",
                        DataInicioInscricao = new DateTime(2024, 02, 2, 7, 30, 0),
                        DataFimInscricao = new DateTime(2024, 02, 7, 12, 30, 0),
                        ValorInscricao = 0,
                        Website = "www.itatechjr.com.br",
                        EmailEvento = "DSI@academico.ufs.br",
                        EventoPublico = 1,
                        Cep = "49506036",
                        Estado = "SE",
                        Cidade = "Itabaiana",
                        Bairro = "Porto",
                        Rua = " Av. Vereador Olímpio Grande",
                        Numero = "s/n",
                        Complemento = "Universidade",
                        PossuiCertificado = 1,
                        FrequenciaMinimaCertificado = 1,
                        IdTipoEvento = 1,
                        VagasOfertadas = 100,
                        VagasReservadas = 35,
                        VagasDisponiveis = 65,
                        TempoMinutosReserva = 240,
                        CargaHoraria = 4,
                    },
                new Evento
                {
                        Id = 3,
                        Nome = "Balada do DJ Ikaruz",
                        Descricao = "Festa Fechada",
                        DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                        DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                        InscricaoGratuita = 1,
                        Status = "C",
                        DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                        DataFimInscricao = new DateTime(2024, 09, 3, 7, 30, 0),
                        ValorInscricao = 0,
                        Website = "www.dj.com.br",
                        EmailEvento = "DJ@gmail.com",
                        EventoPublico = 1,
                        Cep = "49506036",
                        Estado = "SE",
                        Cidade = "Itabaiana",
                        Bairro = "Porto",
                        Rua = " Av. Vereador Olímpio Grande",
                        Numero = "s/n",
                        Complemento = "Universidade",
                        PossuiCertificado = 0,
                        FrequenciaMinimaCertificado = 0,
                        IdTipoEvento = 3,
                        VagasOfertadas = 100,
                        VagasReservadas = 35,
                        VagasDisponiveis = 65,
                        TempoMinutosReserva = 240,
                        CargaHoraria = 12,
                    },
                };

            var pessoas = new List<Pessoa>
                {
                new Pessoa
                {
                        Id = 1,
                        Nome = "João Vitor Sodré",
                        NomeCracha = "Sodré",
                        Cpf = "12246232367",
                        Sexo = "M",
                        Cep = "45340086",
                        Rua = "Avenida Principal",
                        Bairro = "Centro",
                        Cidade = "Irece",
                        Estado = "BA",
                        Numero = "s/n",
                        Complemento = "casa",
                        Email = "email@gmail.com",
                        Telefone1 = "7999900113344",
                        Telefone2 = "NULL",
                    },
                new Pessoa
                {
                        Id = 2,
                        Nome = "Nagibe Santos Wanus Junior",
                        NomeCracha = "Nagibe Junior",
                        Cpf = "12343455678",
                        Sexo = "M",
                        Cep = "45566000",
                        Rua = "Rua Severino Vieira",
                        Bairro = "Centro",
                        Cidade = "Esplanada",
                        Estado = "BA",
                        Numero = "147",
                        Complemento = "casa",
                        Email = "nagibejr@gmail.com",
                        Telefone1 = "75999643467",
                        Telefone2 = "NULL",
                    },
                new Pessoa
                {
                        Id = 3,
                        Nome = "Marcos Venicios da Palma Dias",
                        NomeCracha = "Marcos Venicios",
                        Cpf = "12234544667",
                        Sexo = "M",
                        Cep = "45340086",
                        Rua = "Rua da Linha",
                        Bairro = "Centro",
                        Cidade = "Esplanada",
                        Estado = "BA",
                        Numero = "s/n",
                        Complemento = "casa",
                        Email = "muzanpvp@gmail.com",
                        Telefone1 = "7999900113344",
                        Telefone2 = "NULL",
                    },
                };

            var papel = new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };
            _context.AddRange(pessoas);
            _context.AddRange(papel);
            _context.AddRange(eventos);
            _context.AddRange(inscricoes);
            _context.SaveChanges();

            _inscricaoService = new InscricaoService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _inscricaoService.CreateInscricaoEvento(new Inscricaopessoaevento()
            {
                Id = 4,
                IdPessoa = 3,
                IdEvento = 1,
                IdPapel = 1,
                IdTipoInscricao = 1,
                DataInscricao = new DateTime(2024, 07, 5, 0, 0, 0),
                ValorTotal = 0,
                Status = "S",
                FrequenciaFinal = 0,
            });
            // Assert
            Assert.AreEqual(4, _inscricaoService.GetInscricaoPessoaEvento(1, 1).Count());
            var listaInscricao = _inscricaoService.GetInscricaoPessoaEvento(1,1);
            // Assert
            Assert.IsInstanceOfType(listaInscricao, typeof(IEnumerable<Inscricaopessoaevento>));
            Assert.IsNotNull(listaInscricao);
            Assert.AreEqual(4, listaInscricao.Count());
            var firstInscricao = listaInscricao.First();
            Assert.AreEqual((uint)1, firstInscricao.IdPessoa);
            Assert.AreEqual((uint)1, firstInscricao.IdEvento);
            Assert.AreEqual((int)1, firstInscricao.IdPapel);
            Assert.AreEqual((uint)1, firstInscricao.IdTipoInscricao);
            Assert.AreEqual(DateTime.Parse("2024-07-03 00:00:00"), firstInscricao.DataInscricao);
            Assert.AreEqual((decimal)0, firstInscricao.ValorTotal);
            Assert.AreEqual("A", firstInscricao.Status);
            Assert.AreEqual((decimal)1, firstInscricao.FrequenciaFinal);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _inscricaoService.DeletePessoaPapel(1, 1, 1);
            // Assert
            var inscricoes = _inscricaoService.GetInscricaoPessoaEvento(1, 1);
            Assert.IsNotNull(inscricoes);
            Assert.AreEqual(2, inscricoes.Count());
        }

        [TestMethod()]
        public void GetTest()
        {
            var listaInscricao = _inscricaoService.GetInscricaoPessoaEvento(1, 1);
            Assert.IsInstanceOfType(listaInscricao, typeof(IEnumerable<Inscricaopessoaevento>));
            Assert.IsNotNull(listaInscricao);
            Assert.AreEqual(3, listaInscricao.Count());
            var firstInscricao = listaInscricao.First();
            Assert.AreEqual((uint)1, firstInscricao.IdPessoa);
            Assert.AreEqual((uint)1, firstInscricao.IdEvento);
            Assert.AreEqual((int)1, firstInscricao.IdPapel);
            Assert.AreEqual((uint)1, firstInscricao.IdTipoInscricao);
            Assert.AreEqual(DateTime.Parse("2024-07-03 00:00:00"), firstInscricao.DataInscricao);
            Assert.AreEqual((decimal)0, firstInscricao.ValorTotal);
            Assert.AreEqual("A", firstInscricao.Status);
            Assert.AreEqual((decimal)1, firstInscricao.FrequenciaFinal);
        }
    }
}