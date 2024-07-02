using Service;
using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace Service.Tests
{
    [TestClass()]
    public class AreaInteresseServiceTests
    {

        private EventoContext _context;
        private IAreaInteresseService _areaInteresseService;

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
            var areainteresses = new List<Areainteresse>
                {
                new Areainteresse
                {
                        Id = 1,
                        Nome =  "Curso",
                    },
                new Areainteresse
                {
                        Id = 2,
                        Nome =  "Palestra",
                    },
                new Areainteresse
                {
                        Id = 3,
                        Nome =  "Festival",
                    },
                };

            _context.AddRange(areainteresses);
            _context.SaveChanges();

            _areaInteresseService = new AreaInteresseService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _areaInteresseService.Create(new Areainteresse()
            {
                Id = 4,
                Nome = "Encerramento",
            });
            // Assert
            Assert.AreEqual(4, _areaInteresseService.GetAll().Count());
            var areainteresse = _areaInteresseService.Get(4);
            Assert.AreEqual("Encerramento", areainteresse.Nome);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _areaInteresseService.Delete(1);
            // Assert
            Assert.AreEqual(2, _areaInteresseService.GetAll().Count());
            var areainteresse = _areaInteresseService.Get(1);
            Assert.AreEqual(null, areainteresse);
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var areainteresse = _areaInteresseService.Get(3);
            areainteresse.Id = 3;
            areainteresse.Nome = "Rave";
            //Assert
            areainteresse = _areaInteresseService.Get(3);
            Assert.AreEqual((uint)3, areainteresse.Id);
            Assert.AreEqual("Rave", areainteresse.Nome);
        }

        [TestMethod()]
        public void GetTest()
        {
            var areainteresse = _areaInteresseService.Get(2);
            Assert.IsNotNull(_areaInteresseService);
            Assert.AreEqual((uint)2, areainteresse.Id);
            Assert.AreEqual("Palestra", areainteresse.Nome);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaAreaInteresse = _areaInteresseService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaAreaInteresse, typeof(IEnumerable<Areainteresse>));
            Assert.IsNotNull(listaAreaInteresse);
            Assert.AreEqual(3, listaAreaInteresse.Count());
            var firstAreainteresse = listaAreaInteresse.First();
            Assert.AreEqual((uint)1, firstAreainteresse.Id);
        }
    }
}