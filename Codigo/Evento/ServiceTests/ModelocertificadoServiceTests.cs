using Core.Service;
using Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Service.Tests
{
    [TestClass]
    public class ModelocertificadoServiceTests
    {
        private EventoContext _context;
        private IModelocertificadoService _modelocertificadoService;

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

            // Criar eventos para os modelos de certificado
            var eventos = new List<Evento>
            {
                new Evento { Id = 1, Nome = "Evento 1", Status = "A", Descricao = "Descrição do Evento 1", DataInicio = DateTime.Now, DataFim = DateTime.Now.AddDays(1) },
                new Evento { Id = 2, Nome = "Evento 2", Status = "A", Descricao = "Descrição do Evento 2", DataInicio = DateTime.Now, DataFim = DateTime.Now.AddDays(1) },
                new Evento { Id = 3, Nome = "Evento 3", Status = "A", Descricao = "Descrição do Evento 3", DataInicio = DateTime.Now, DataFim = DateTime.Now.AddDays(1) }
            };
            _context.AddRange(eventos);

            var modelocertificados = new List<Modelocertificado>
            {
                new Modelocertificado
                {
                    Id = 1,
                    IdEvento = 1,
                    LogotipoSuperior = new byte[] { 0x20, 0x20 },
                    TextoAntesParticipante = "Certificamos que",
                    TextoAntesEvento = "participou do evento",
                    TextoAntesCargaHoraria = "com carga horária de",
                    Assinatura1Texto = "Assinatura 1",
                    Assinatura1 = new byte[] { 0x21, 0x21 }
                },
                new Modelocertificado
                {
                    Id = 2,
                    IdEvento = 2,
                    LogotipoSuperior = new byte[] { 0x30, 0x30 },
                    TextoAntesParticipante = "Certificamos que",
                    TextoAntesEvento = "participou do evento",
                    TextoAntesCargaHoraria = "com carga horária de",
                    Assinatura1Texto = "Assinatura 1",
                    Assinatura1 = new byte[] { 0x31, 0x31 },
                    Assinatura2Texto = "Assinatura 2",
                    Assinatura2 = new byte[] { 0x32, 0x32 }
                },
                new Modelocertificado
                {
                    Id = 3,
                    IdEvento = 3,
                    LogotipoSuperior = new byte[] { 0x40, 0x40 },
                    TextoAntesParticipante = "Certificamos que",
                    TextoAntesEvento = "participou do evento",
                    TextoAntesCargaHoraria = "com carga horária de",
                    Assinatura1Texto = "Assinatura 1",
                    Assinatura1 = new byte[] { 0x41, 0x41 }
                }
            };

            _context.AddRange(modelocertificados);
            _context.SaveChanges();

            _modelocertificadoService = new ModelocertificadoService(_context);
        }

        [TestMethod]
        public void CreateTest()
        {
            // Act
            var novoModelo = new Modelocertificado
            {
                Id = 4,
                IdEvento = 1,
                LogotipoSuperior = new byte[] { 0x50, 0x50 },
                TextoAntesParticipante = "Certificamos que",
                TextoAntesEvento = "participou do evento",
                TextoAntesCargaHoraria = "com carga horária de",
                Assinatura1Texto = "Assinatura 1",
                Assinatura1 = new byte[] { 0x51, 0x51 }
            };

            _modelocertificadoService.Create(novoModelo);

            // Assert
            Assert.AreEqual(4, _modelocertificadoService.GetAll().Count());
            var modelocertificado = _modelocertificadoService.Get(4);
            Assert.IsNotNull(modelocertificado);
            Assert.AreEqual((uint)1, modelocertificado.IdEvento);
            CollectionAssert.AreEqual(new byte[] { 0x50, 0x50 }, modelocertificado.LogotipoSuperior);
            Assert.AreEqual("Certificamos que", modelocertificado.TextoAntesParticipante);
        }

        [TestMethod]
        public void DeleteTest()
        {
            // Act
            _modelocertificadoService.Delete(1);

            // Assert
            Assert.AreEqual(2, _modelocertificadoService.GetAll().Count());
            var modelocertificado = _modelocertificadoService.Get(1);
            Assert.IsNull(modelocertificado);
        }

        [TestMethod]
        public void UpdateTest()
        {
            // Act 
            var modelocertificado = _modelocertificadoService.Get(2);
            _context.Entry(modelocertificado).State = EntityState.Detached;
            modelocertificado.TextoAntesParticipante = "Certificamos que o participante";
            modelocertificado.TextoAntesEvento = "participou com sucesso do evento";
            _modelocertificadoService.Update(modelocertificado);

            // Assert
            modelocertificado = _modelocertificadoService.Get(2);
            Assert.AreEqual("Certificamos que o participante", modelocertificado.TextoAntesParticipante);
            Assert.AreEqual("participou com sucesso do evento", modelocertificado.TextoAntesEvento);
        }

        [TestMethod]
        public void GetTest()
        {
            var modelocertificado = _modelocertificadoService.Get(2);
            Assert.IsNotNull(modelocertificado);
            Assert.AreEqual((uint)2, modelocertificado.IdEvento);
            Assert.AreEqual("Certificamos que", modelocertificado.TextoAntesParticipante);
            CollectionAssert.AreEqual(new byte[] { 0x30, 0x30 }, modelocertificado.LogotipoSuperior);
        }

        [TestMethod]
        public void GetAllTest()
        {
            // Act
            var listaModelocertificado = _modelocertificadoService.GetAll();

            // Assert
            Assert.IsInstanceOfType(listaModelocertificado, typeof(IEnumerable<Modelocertificado>));
            Assert.IsNotNull(listaModelocertificado);
            Assert.AreEqual(3, listaModelocertificado.Count());
            var firstModelocertificado = listaModelocertificado.First();
            Assert.AreEqual((uint)1, firstModelocertificado.IdEvento);
            Assert.AreEqual("Certificamos que", firstModelocertificado.TextoAntesParticipante);
        }

        [TestMethod]
        public void GetByEventoTest()
        {
            // Act
            var modelocertificados = _modelocertificadoService.GetByEvento(1);

            // Assert
            Assert.IsNotNull(modelocertificados);
            Assert.AreEqual(1, modelocertificados.Count());
            var modelocertificado = modelocertificados.First();
            Assert.AreEqual((uint)1, modelocertificado.IdEvento);
            Assert.AreEqual("Certificamos que", modelocertificado.TextoAntesParticipante);
            CollectionAssert.AreEqual(new byte[] { 0x20, 0x20 }, modelocertificado.LogotipoSuperior);
        }
    }
} 