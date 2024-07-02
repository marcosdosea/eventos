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
    public class TipoinscricaoServiceTests
    {
        private EventoContext _context;
        private ITipoInscricaoService _tipoInscricaoService;

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
            var tipoinscricaos = new List<Tipoinscricao>
                {
                new Tipoinscricao
                {
                        Id = 1,
                        IdEvento = 1,
                        Nome = "Gratuita",
                        Descricao = "Incrição sem cobrança",
                        Valor = 0,
                        DataInicio = new DateTime(2024, 02, 2, 7, 30, 0),
                        Datafim = new DateTime(2024, 02, 7, 12, 30, 0),
                        UsadaEvento = 1,
                        UsadaSubevento = 1,
                    },
                new Tipoinscricao
                {
                        Id = 2,
                        IdEvento = 3,
                        Nome = "Paga",
                        Descricao = "Incrição com cobrança",
                        Valor = 20,
                        DataInicio = new DateTime(2024, 05, 2, 7, 30, 0),
                        Datafim = new DateTime(2024, 05, 7, 12, 30, 0),
                        UsadaEvento = 1,
                        UsadaSubevento = 1,
                    },
                new Tipoinscricao
                {
                        Id = 3,
                        IdEvento = 5,
                        Nome = "Meia Entrada",
                        Descricao = "Incrição com metade da cobrança",
                        Valor = 10,
                        DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                        Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                        UsadaEvento = 1,
                        UsadaSubevento = 1,
                    },
                };

            _context.AddRange(tipoinscricaos);
            _context.SaveChanges();

            _tipoInscricaoService = new TipoInscricaoService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _tipoInscricaoService.Create(new Tipoinscricao()
            {
                Id = 4,
                IdEvento = 5,
                Nome = "Meia Entrada",
                Descricao = "Incrição com metade da cobrança",
                Valor = 10,
                DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                UsadaEvento = 1,
                UsadaSubevento = 1,
            });
            // Assert
            Assert.AreEqual(4, _tipoInscricaoService.GetAll().Count());
            var tipoinscricao = _tipoInscricaoService.Get(3);
            Assert.AreEqual((uint)5, tipoinscricao.IdEvento);
            Assert.AreEqual("Meia Entrada", tipoinscricao.Nome);
            Assert.AreEqual("Incrição com metade da cobrança", tipoinscricao.Descricao);
            Assert.AreEqual((decimal)10, tipoinscricao.Valor);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), tipoinscricao.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), tipoinscricao.Datafim);
            Assert.AreEqual((sbyte)1, tipoinscricao.UsadaEvento);
            Assert.AreEqual((sbyte)1, tipoinscricao.UsadaEvento);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _tipoInscricaoService.Delete(1);
            // Assert
            Assert.AreEqual(2, _tipoInscricaoService.GetAll().Count());
            var tipoinscricao = _tipoInscricaoService.Get(1);
            Assert.AreEqual(null, tipoinscricao);
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var tipoinscricao = _tipoInscricaoService.Get(3);
            tipoinscricao.Id = 2;
            tipoinscricao.IdEvento = 3;
            tipoinscricao.Nome = "Paga";
            tipoinscricao.Descricao = "Incrição com cobrança";
            tipoinscricao.Valor = 20;
            tipoinscricao.DataInicio = new DateTime(2024, 05, 2, 7, 30, 0);
            tipoinscricao.Datafim = new DateTime(2024, 05, 7, 12, 30, 0);
            tipoinscricao.UsadaEvento = 1;
            tipoinscricao.UsadaSubevento = 1;
            //Assert
            tipoinscricao = _tipoInscricaoService.Get(2);
            Assert.AreEqual((uint)3, tipoinscricao.IdEvento);
            Assert.AreEqual("Paga", tipoinscricao.Nome);
            Assert.AreEqual("Incrição com cobrança", tipoinscricao.Descricao);
            Assert.AreEqual((decimal)20, tipoinscricao.Valor);
            Assert.AreEqual(DateTime.Parse("2024-05-02 07:30:00"), tipoinscricao.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-05-07 12:30:00"), tipoinscricao.Datafim);
            Assert.AreEqual((sbyte)1, tipoinscricao.UsadaEvento);
            Assert.AreEqual((sbyte)1, tipoinscricao.UsadaEvento);
        }

        [TestMethod()]
        public void GetTest()
        {
            var tipoinscricao = _tipoInscricaoService.Get(2);
            Assert.IsNotNull(tipoinscricao);
            Assert.AreEqual((uint)3, tipoinscricao.IdEvento);
            Assert.AreEqual("Paga", tipoinscricao.Nome);
            Assert.AreEqual("Incrição com cobrança", tipoinscricao.Descricao);
            Assert.AreEqual((decimal)20, tipoinscricao.Valor);
            Assert.AreEqual(DateTime.Parse("2024-05-02 07:30:00"), tipoinscricao.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-05-07 12:30:00"), tipoinscricao.Datafim);
            Assert.AreEqual((sbyte)1, tipoinscricao.UsadaEvento);
            Assert.AreEqual((sbyte)1, tipoinscricao.UsadaEvento);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaTipoinscricao = _tipoInscricaoService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaTipoinscricao, typeof(IEnumerable<Tipoinscricao>));
            Assert.IsNotNull(listaTipoinscricao);
            Assert.AreEqual(3, listaTipoinscricao.Count());
            var firstTipoinscricao = listaTipoinscricao.First();
            Assert.AreEqual((uint)1, firstTipoinscricao.IdEvento);
            Assert.AreEqual("Gratuita", firstTipoinscricao.Nome);
            Assert.AreEqual("Incrição sem cobrança", firstTipoinscricao.Descricao);
            Assert.AreEqual((decimal)0, firstTipoinscricao.Valor);
            Assert.AreEqual(DateTime.Parse("2024-02-02 07:30:00"), firstTipoinscricao.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-02-07 12:30:00"), firstTipoinscricao.Datafim);
            Assert.AreEqual((sbyte)1, firstTipoinscricao.UsadaEvento);
            Assert.AreEqual((sbyte)1, firstTipoinscricao.UsadaEvento);
        }
    }
}