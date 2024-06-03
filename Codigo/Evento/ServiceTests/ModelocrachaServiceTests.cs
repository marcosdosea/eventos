using Core.Service;
using Core;


namespace Service.Tests
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;

    [TestClass]
    public class ModelocrachaServiceTests
    {
        private EventoContext _context;
        private IModelocrachaService _modelocrachaService;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var builder = new DbContextOptionsBuilder<EventoContext>();
            builder.UseInMemoryDatabase("evento");
            var options = builder.Options;

            _context = new EventoContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var modelocrachas = new List<Modelocracha>
        {
            new Modelocracha
            {
                Id = 1,
                IdEvento = 1,
                Logotipo = new byte[] { 0x20, 0x20 }, 
                Texto = "Texto 1",
                Qrcode = 1
            },
            new Modelocracha
            {
                Id = 3,
                IdEvento = 2,
                Logotipo = new byte[] { 0x30, 0x30 }, 
                Texto = "Texto 2",
                Qrcode = 1
            },
            new Modelocracha
            {
                Id = 5,
                IdEvento = 3,
                Logotipo = new byte[] { 0x50, 0x50 }, 
                Texto = "Texto 3",
                Qrcode = 1
            }
        };

            _context.AddRange(modelocrachas);
            _context.SaveChanges();

            _modelocrachaService = new ModelocrachaService(_context);
        }

        [TestMethod]
        public void CreateTest()
        {
            // Act
            _modelocrachaService.Create(new Modelocracha()
            {
                Id = 7,
                IdEvento = 4,
                Logotipo = new byte[] { 0x70, 0x70 }, 
                Texto = "Texto 4",
                Qrcode = 1
            });

            // Assert
            Assert.AreEqual(4, _modelocrachaService.GetAll().Count());
            var modelocracha = _modelocrachaService.Get(7);
            Assert.IsNotNull(modelocracha);
            Assert.AreEqual((uint)4, modelocracha.IdEvento);
            CollectionAssert.AreEqual(new byte[] { 0x70, 0x70 }, modelocracha.Logotipo);
        }

        [TestMethod]
        public void DeleteTest()
        {
            // Act
            _modelocrachaService.Delete(1);

            // Assert
            Assert.AreEqual(2, _modelocrachaService.GetAll().Count());
            var modelocracha = _modelocrachaService.Get(1);
            Assert.IsNull(modelocracha);
        }

        [TestMethod]
        public void EditTest()
        {
            // Act 
            var modelocracha = _modelocrachaService.Get(3);
            modelocracha.IdEvento = 2;
            modelocracha.Texto = "Texto Editado";
            modelocracha.Logotipo = new byte[] { 0x60, 0x60 }; 
            _modelocrachaService.Edit(modelocracha);

            // Assert
            modelocracha = _modelocrachaService.Get(3);
            Assert.AreEqual((uint)2, modelocracha.IdEvento);
            Assert.AreEqual("Texto Editado", modelocracha.Texto);
            CollectionAssert.AreEqual(new byte[] { 0x60, 0x60 }, modelocracha.Logotipo);
        }

        [TestMethod]
        public void GetTest()
        {
            var modelocracha = _modelocrachaService.Get(3);
            Assert.IsNotNull(modelocracha);
            Assert.AreEqual((uint)2, modelocracha.IdEvento);
            CollectionAssert.AreEqual(new byte[] { 0x30, 0x30 }, modelocracha.Logotipo);
        }

        [TestMethod]
        public void GetAllTest()
        {
            // Act
            var listaModelocracha = _modelocrachaService.GetAll();

            // Assert
            Assert.IsInstanceOfType(listaModelocracha, typeof(IEnumerable<Modelocracha>));
            Assert.IsNotNull(listaModelocracha);
            Assert.AreEqual(3, listaModelocracha.Count());
            var firstModelocracha = listaModelocracha.First();
            Assert.AreEqual((uint)1, firstModelocracha.IdEvento);
            CollectionAssert.AreEqual(new byte[] { 0x20, 0x20 }, firstModelocracha.Logotipo);
        }
    }

}