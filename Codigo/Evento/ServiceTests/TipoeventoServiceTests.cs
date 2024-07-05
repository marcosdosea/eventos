using Service;
using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace Service.Tests
{
    [TestClass()]
    public class TipoeventoServiceTests
    {

        private EventoContext _context;
        private ITipoeventoService _tipoeventoService;

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
            var tipoeventos = new List<Tipoevento>
                {
                new Tipoevento
                {
                        Id = 1,
                        Nome =  "Curso",
                    },
                new Tipoevento
                {
                        Id = 2,
                        Nome =  "Palestra",
                    },
                new Tipoevento
                {
                        Id = 3,
                        Nome =  "Festival",
                    },
                };

            _context.AddRange(tipoeventos);
            _context.SaveChanges();

            _tipoeventoService = new TipoeventoService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _tipoeventoService.Create(new Tipoevento()
            {
                Id = 4,
                Nome = "Encerramento",
            });
            // Assert
            Assert.AreEqual(4, _tipoeventoService.GetAll().Count());
            var tipoevento = _tipoeventoService.Get(4);
            Assert.AreEqual("Encerramento", tipoevento.Nome);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _tipoeventoService.Delete(1);
            // Assert
            Assert.AreEqual(2, _tipoeventoService.GetAll().Count());
            var tipoevento = _tipoeventoService.Get(1);
            Assert.AreEqual(null, tipoevento);
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var tipoevento = _tipoeventoService.Get(3);
            tipoevento.Id = 3;
            tipoevento.Nome = "Rave";
            //Assert
            tipoevento = _tipoeventoService.Get(3);
            Assert.AreEqual((uint)3, tipoevento.Id);
            Assert.AreEqual("Rave", tipoevento.Nome);
        }

        [TestMethod()]
        public void GetTest()
        {
            var tipoevento = _tipoeventoService.Get(2);
            Assert.IsNotNull(_tipoeventoService);
            Assert.AreEqual((uint)2, tipoevento.Id);
            Assert.AreEqual("Palestra", tipoevento.Nome);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaTipoevento = _tipoeventoService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaTipoevento, typeof(IEnumerable<Tipoevento>));
            Assert.IsNotNull(listaTipoevento);
            Assert.AreEqual(3, listaTipoevento.Count());
            var firstTipoevento = listaTipoevento.First();
            Assert.AreEqual((uint)1, firstTipoevento.Id);
        }
    }
}