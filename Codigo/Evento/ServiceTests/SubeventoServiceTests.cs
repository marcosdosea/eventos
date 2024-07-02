using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Service.Tests
{
    [TestClass()]
    public class SubeventoServiceTests
    {
        private EventoContext _context;
        private ISubeventoService _subeventoService;

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
            var subeventos = new List<Subevento>
                {
                new Subevento
                {
                    Id = 1,
                    IdEvento = 1,
                    Nome = "SEMINFO",
                    Descricao = "Evento para a semana da tecnologia",
                    DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 09, 7, 12, 30, 0),
                    InscricaoGratuita = 1,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                    ValorInscricao = 0,
                    PossuiCertificado = 1,
                    FrequenciaMinimaCertificado = 1,
                    IdTipoEvento = 1,
                    VagasOfertadas = 100,
                    VagasReservadas = 35,
                    VagasDisponiveis = 65,
                    CargaHoraria = 4,
                    },
                new Subevento
                {
                        Id = 3,
                        IdEvento = 2,
                        Nome = "SEMAC",
                        Descricao = "Semana academica de cursos",
                        DataInicio = new DateTime(2024, 02, 2, 7, 30, 0),
                        DataFim = new DateTime(2024, 09, 7, 12, 30, 0),
                        InscricaoGratuita = 1,
                        Status = "F",
                        DataInicioInscricao = new DateTime(2024, 02, 2, 7, 30, 0),
                        DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                        ValorInscricao = 0,
                        PossuiCertificado = 1,
                        FrequenciaMinimaCertificado = 1,
                        IdTipoEvento = 1,
                        VagasOfertadas = 100,
                        VagasReservadas = 35,
                        VagasDisponiveis = 65,
                        CargaHoraria = 4,
                    },
                new Subevento
                {
                        Id = 5,
                        IdEvento = 3,
                        Nome = "Balada do DJ Ikaruz",
                        Descricao = "Festa Fechada",
                        DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                        DataFim = new DateTime(2024, 09, 3, 7, 30, 0),
                        InscricaoGratuita = 1,
                        Status = "C",
                        DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                        DataFimInscricao = new DateTime(2024, 09, 3, 7, 30, 0),
                        ValorInscricao = 0,
                        PossuiCertificado = 0,
                        FrequenciaMinimaCertificado = 0,
                        IdTipoEvento = 3,
                        VagasOfertadas = 100,
                        VagasReservadas = 35,
                        VagasDisponiveis = 65,
                        CargaHoraria = 12,
                    },
                };

            _context.AddRange(subeventos);
            _context.SaveChanges();

            _subeventoService = new SubeventoService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _subeventoService.Create(new Subevento()
            {
                Id = 7,
                IdEvento = 3,
                Nome = "Balada do DJ Ikaruz",
                Descricao = "Festa Fechada",
                DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                DataFim = new DateTime(2024, 09, 3, 7, 30, 0),
                InscricaoGratuita = 1,
                Status = "C",
                DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 09, 3, 7, 30, 0),
                ValorInscricao = 0,
                PossuiCertificado = 0,
                FrequenciaMinimaCertificado = 0,
                IdTipoEvento = 3,
                VagasOfertadas = 100,
                VagasReservadas = 35,
                VagasDisponiveis = 65,
                CargaHoraria = 12,
            });
            // Assert
            Assert.AreEqual(4, _subeventoService.GetAll().Count());
            var subevento = _subeventoService.Get(7);
            Assert.AreEqual((uint)3, subevento.IdEvento);
            Assert.AreEqual("Balada do DJ Ikaruz", subevento.Nome);
            Assert.AreEqual("Festa Fechada", subevento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subevento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-03 07:30:00"), subevento.DataFim);
            Assert.AreEqual((sbyte)1, subevento.InscricaoGratuita);
            Assert.AreEqual("C", subevento.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subevento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-03 07:30:00"), subevento.DataFimInscricao);
            Assert.AreEqual((decimal)0, subevento.ValorInscricao);
            Assert.AreEqual((sbyte)0, subevento.PossuiCertificado);
            Assert.AreEqual((decimal)0, subevento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)3, subevento.IdTipoEvento);
            Assert.AreEqual((int)100, subevento.VagasOfertadas);
            Assert.AreEqual((int)35, subevento.VagasReservadas);
            Assert.AreEqual((int)65, subevento.VagasDisponiveis);
            Assert.AreEqual((int)12, subevento.CargaHoraria);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _subeventoService.Delete(1);
            // Assert
            Assert.AreEqual(2, _subeventoService.GetAll().Count());
            var subevento = _subeventoService.Get(7);
            Assert.AreEqual(null, subevento);
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var subevento = _subeventoService.Get(3);
            subevento.IdEvento = 2;
            subevento.Nome = "SEMAC";
            subevento.Descricao = "Semana academica de cursos";
            subevento.DataInicio = new DateTime(2024, 02, 2, 7, 30, 0);
            subevento.DataFim = new DateTime(2024, 09, 7, 12, 30, 0);
            subevento.InscricaoGratuita = 1;
            subevento.Status = "F";
            subevento.DataInicioInscricao = new DateTime(2024, 02, 2, 7, 30, 0);
            subevento.DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0);
            subevento.ValorInscricao = 0;
            subevento.PossuiCertificado = 1;
            subevento.FrequenciaMinimaCertificado = 1;
            subevento.IdTipoEvento = 1;
            subevento.VagasOfertadas = 100;
            subevento.VagasReservadas = 35;
            subevento.VagasDisponiveis = 65;
            subevento.CargaHoraria = 4;
            _subeventoService.Edit(subevento);
            //Assert
            subevento = _subeventoService.Get(3);
            Assert.AreEqual((uint)2, subevento.IdEvento);
            Assert.AreEqual("SEMAC", subevento.Nome);
            Assert.AreEqual("Semana academica de cursos", subevento.Descricao);
            Assert.AreEqual(new DateTime(2024, 02, 02, 7, 30, 0), subevento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subevento.DataFim);
            Assert.AreEqual((sbyte)1, subevento.InscricaoGratuita);
            Assert.AreEqual("F", subevento.Status);
            Assert.AreEqual(new DateTime(2024, 02, 02, 7, 30, 0), subevento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subevento.DataFimInscricao);
            Assert.AreEqual((decimal)0, subevento.ValorInscricao);
            Assert.AreEqual((sbyte)1, subevento.PossuiCertificado);
            Assert.AreEqual((decimal)1, subevento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, subevento.IdTipoEvento);
            Assert.AreEqual((int)100, subevento.VagasOfertadas);
            Assert.AreEqual((int)35, subevento.VagasReservadas);
            Assert.AreEqual((int)65, subevento.VagasDisponiveis);
            Assert.AreEqual((int)4, subevento.CargaHoraria);
        }

        [TestMethod()]
        public void GetTest()
        {
            var subevento = _subeventoService.Get(3);
            Assert.IsNotNull(subevento);
            Assert.AreEqual((uint)2, subevento.IdEvento);
            Assert.AreEqual("SEMAC", subevento.Nome);
            Assert.AreEqual("Semana academica de cursos", subevento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-02-02 07:30:00"), subevento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subevento.DataFim);
            Assert.AreEqual((sbyte)1, subevento.InscricaoGratuita);
            Assert.AreEqual("F", subevento.Status);
            Assert.AreEqual(DateTime.Parse("2024-02-02 07:30:00"), subevento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subevento.DataFimInscricao);
            Assert.AreEqual((decimal)0, subevento.ValorInscricao);
            Assert.AreEqual((sbyte)1, subevento.PossuiCertificado);
            Assert.AreEqual((decimal)1, subevento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, subevento.IdTipoEvento);
            Assert.AreEqual((int)100, subevento.VagasOfertadas);
            Assert.AreEqual((int)35, subevento.VagasReservadas);
            Assert.AreEqual((int)65, subevento.VagasDisponiveis);
            Assert.AreEqual((int)4, subevento.CargaHoraria);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaSubevento = _subeventoService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaSubevento, typeof(IEnumerable<Subevento>));
            Assert.IsNotNull(listaSubevento);
            Assert.AreEqual(3, listaSubevento.Count());
            var firstSubevento = listaSubevento.First();
            Assert.AreEqual((uint)1, firstSubevento.IdEvento);
            Assert.AreEqual("SEMINFO", firstSubevento.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", firstSubevento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), firstSubevento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), firstSubevento.DataFim);
            Assert.AreEqual((sbyte)1, firstSubevento.InscricaoGratuita);
            Assert.AreEqual("A", firstSubevento.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), firstSubevento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), firstSubevento.DataFimInscricao);
            Assert.AreEqual((decimal)0, firstSubevento.ValorInscricao);
            Assert.AreEqual((sbyte)1, firstSubevento.PossuiCertificado);
            Assert.AreEqual((decimal)1, firstSubevento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, firstSubevento.IdTipoEvento);
            Assert.AreEqual((int)100, firstSubevento.VagasOfertadas);
            Assert.AreEqual((int)35, firstSubevento.VagasReservadas);
            Assert.AreEqual((int)65, firstSubevento.VagasDisponiveis);
            Assert.AreEqual((int)4, firstSubevento.CargaHoraria);
        }
    }
}