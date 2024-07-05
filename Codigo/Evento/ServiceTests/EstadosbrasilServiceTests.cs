using Service;
using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace Service.Tests
{
    [TestClass()]
    public class EstadosbrasilServiceTests
    {

        private EventoContext _context;
        private IEstadosbrasilService _estadosbrasilService;

        [TestInitialize]
        public void Initialize()
        {
            //Arrange
            var builder = new DbContextOptionsBuilder<EventoContext>();
            builder.UseInMemoryDatabase("evento");
            var options = builder.Options;

            _context = new EventoContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            var estadosbrasil = new List<Estadosbrasil>
                {
                new Estadosbrasil
                {
                        Estado = "SE",
                        Nome =  "Sergipe",
                    },
                new Estadosbrasil
                {
                        Estado = "BA",
                        Nome =  "Bahia",
                    },
                new Estadosbrasil
                {
                        Estado = "AL",
                        Nome =  "Alagoas",
                    },
                };

            _context.AddRange(estadosbrasil);
            _context.SaveChanges();

            _estadosbrasilService = new EstadosbrasilService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _estadosbrasilService.Create(new Estadosbrasil()
            {
                Estado = "PE",
                Nome = "Pernambuco",
            });
            // Assert
            Assert.AreEqual(4, _estadosbrasilService.GetAll().Count());
            var estadobrasil = _estadosbrasilService.Get("PE");
            Assert.AreEqual("Pernambuco", estadobrasil.Nome);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _estadosbrasilService.Delete("SE");
            // Assert
            Assert.AreEqual(2, _estadosbrasilService.GetAll().Count());
            var estadobrasil = _estadosbrasilService.Get("SE");
            Assert.AreEqual(null, estadobrasil);
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var estadobrasil = _estadosbrasilService.Get("AL");
            estadobrasil.Estado = "AL";
            estadobrasil.Nome = "Alagoas";
            //Assert
            estadobrasil = _estadosbrasilService.Get("AL");
            Assert.AreEqual("AL", estadobrasil.Estado);
            Assert.AreEqual("Alagoas", estadobrasil.Nome);
        }

        [TestMethod()]
        public void GetTest()
        {
            var estadobrasil = _estadosbrasilService.Get("BA");
            Assert.IsNotNull(_estadosbrasilService);
            Assert.AreEqual("BA", estadobrasil.Estado);
            Assert.AreEqual("Bahia", estadobrasil.Nome);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaEstadobrasil = _estadosbrasilService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaEstadobrasil, typeof(IEnumerable<Estadosbrasil>));
            Assert.IsNotNull(listaEstadobrasil);
            Assert.AreEqual(3, listaEstadobrasil.Count());
            var firstTipoevento = listaEstadobrasil.First();
            Assert.AreEqual("SE", firstTipoevento.Estado);
        }
    }
}