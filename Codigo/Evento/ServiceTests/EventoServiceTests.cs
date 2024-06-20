using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Tests
{
    [TestClass()]
    public class EventoServiceTests
    {
        private EventoContext _context;
        private IEventoService _eventoService;
        private IPessoaService _pessoaService;
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

            _context.AddRange(eventos);
            _context.SaveChanges();

            _eventoService = new EventoService(_context, _pessoaService, _inscricaoService);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _eventoService.Create(new Evento()
            {
                Id = 4,
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
            });
            // Assert
            Assert.AreEqual(4, _eventoService.GetAll().Count());
            var evento = _eventoService.Get(4);
            Assert.AreEqual("Balada do DJ Ikaruz", evento.Nome);
            Assert.AreEqual("Festa Fechada", evento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), evento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), evento.DataFim);
            Assert.AreEqual((sbyte)1, evento.InscricaoGratuita);
            Assert.AreEqual("C", evento.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), evento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-03 07:30:00"), evento.DataFimInscricao);
            Assert.AreEqual((decimal)0, evento.ValorInscricao);
            Assert.AreEqual("www.dj.com.br", evento.Website);
            Assert.AreEqual("DJ@gmail.com", evento.EmailEvento);
            Assert.AreEqual((sbyte)1, evento.EventoPublico);
            Assert.AreEqual("49506036", evento.Cep);
            Assert.AreEqual("SE", evento.Estado);
            Assert.AreEqual("Itabaiana", evento.Cidade);
            Assert.AreEqual("Porto", evento.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", evento.Rua);
            Assert.AreEqual("s/n", evento.Numero);
            Assert.AreEqual("Universidade", evento.Complemento);
            Assert.AreEqual((sbyte)0, evento.PossuiCertificado);
            Assert.AreEqual((decimal)0, evento.FrequenciaMinimaCertificado);
            Assert.AreEqual((int)3, evento.IdTipoEvento);
            Assert.AreEqual((int)100, evento.VagasOfertadas);
            Assert.AreEqual((int)35, evento.VagasReservadas);
            Assert.AreEqual((int)65, evento.VagasDisponiveis);
            Assert.AreEqual((int)240, evento.TempoMinutosReserva);
            Assert.AreEqual((int)12, evento.CargaHoraria);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _eventoService.Delete(1);
            // Assert
            Assert.AreEqual(2, _eventoService.GetAll().Count());
            var evento = _eventoService.Get(1);
            Assert.AreEqual(null, evento);
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var evento = _eventoService.Get(2);
            evento.Id = 2;
            evento.Nome = "SEMAC";
            evento.Descricao = "Semana academica de cursos";
            evento.DataInicio = new DateTime(2024, 10, 1, 7, 30, 0);
            evento.DataFim = new DateTime(2024, 10, 7, 12, 30, 0);
            evento.InscricaoGratuita = 1;
            evento.Status = "F";
            evento.DataInicioInscricao = new DateTime(2024, 02, 1, 7, 30, 0);
            evento.DataFimInscricao = new DateTime(2024, 02, 7, 12, 30, 0);
            evento.ValorInscricao = 0;
            evento.Website = "www.itatechjr.com.br";
            evento.EmailEvento = "DSI@academico.ufs.br";
            evento.EventoPublico = 1;
            evento.Cep = "49506036";
            evento.Estado = "SE";
            evento.Cidade = "Itabaiana";
            evento.Bairro = "Porto";
            evento.Rua = " Av. Vereador Olímpio Grande";
            evento.Numero = "s/n";
            evento.Complemento = "Universidade";
            evento.PossuiCertificado = 1;
            evento.FrequenciaMinimaCertificado = 1;
            evento.IdTipoEvento = 1;
            evento.VagasOfertadas = 100;
            evento.VagasReservadas = 35;
            evento.VagasDisponiveis = 65;
            evento.TempoMinutosReserva = 240;
            evento.CargaHoraria = 4;
            _eventoService.Edit(evento);
            //Assert
            Assert.AreEqual((uint)2, evento.Id);
            Assert.AreEqual("SEMAC", evento.Nome);
            Assert.AreEqual("Semana academica de cursos", evento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-01 07:30:00"), evento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), evento.DataFim);
            Assert.AreEqual((sbyte)1, evento.InscricaoGratuita);
            Assert.AreEqual("F", evento.Status);
            Assert.AreEqual(new DateTime(2024, 02, 1, 7, 30, 0), evento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-02-07 12:30:00"), evento.DataFimInscricao);
            Assert.AreEqual((decimal)0, evento.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", evento.Website);
            Assert.AreEqual("DSI@academico.ufs.br", evento.EmailEvento);
            Assert.AreEqual((sbyte)1, evento.EventoPublico);
            Assert.AreEqual("49506036", evento.Cep);
            Assert.AreEqual("SE", evento.Estado);
            Assert.AreEqual("Itabaiana", evento.Cidade);
            Assert.AreEqual("Porto", evento.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", evento.Rua);
            Assert.AreEqual("s/n", evento.Numero);
            Assert.AreEqual("Universidade", evento.Complemento);
            Assert.AreEqual((sbyte)1, evento.PossuiCertificado);
            Assert.AreEqual((decimal)1, evento.FrequenciaMinimaCertificado);
            Assert.AreEqual((int)1, evento.IdTipoEvento);
            Assert.AreEqual((int)100, evento.VagasOfertadas);
            Assert.AreEqual((int)35, evento.VagasReservadas);
            Assert.AreEqual((int)65, evento.VagasDisponiveis);
            Assert.AreEqual((int)240, evento.TempoMinutosReserva);
            Assert.AreEqual((int)4, evento.CargaHoraria);
        }

        [TestMethod()]
        public void GetTest()
        {
            var evento = _eventoService.Get(2);
            Assert.AreEqual((uint)2, evento.Id);
            Assert.AreEqual("SEMAC", evento.Nome);
            Assert.AreEqual("Semana academica de cursos", evento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), evento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), evento.DataFim);
            Assert.AreEqual((sbyte)1, evento.InscricaoGratuita);
            Assert.AreEqual("F", evento.Status);
            Assert.AreEqual(DateTime.Parse("2024-02-02 07:30:00"), evento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-02-07 12:30:00"), evento.DataFimInscricao);
            Assert.AreEqual((decimal)0, evento.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", evento.Website);
            Assert.AreEqual("DSI@academico.ufs.br", evento.EmailEvento);
            Assert.AreEqual((sbyte)1, evento.EventoPublico);
            Assert.AreEqual("49506036", evento.Cep);
            Assert.AreEqual("SE", evento.Estado);
            Assert.AreEqual("Itabaiana", evento.Cidade);
            Assert.AreEqual("Porto", evento.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", evento.Rua);
            Assert.AreEqual("s/n", evento.Numero);
            Assert.AreEqual("Universidade", evento.Complemento);
            Assert.AreEqual((sbyte)1, evento.PossuiCertificado);
            Assert.AreEqual((decimal)1, evento.FrequenciaMinimaCertificado);
            Assert.AreEqual((int)1, evento.IdTipoEvento);
            Assert.AreEqual((int)100, evento.VagasOfertadas);
            Assert.AreEqual((int)35, evento.VagasReservadas);
            Assert.AreEqual((int)65, evento.VagasDisponiveis);
            Assert.AreEqual((int)240, evento.TempoMinutosReserva);
            Assert.AreEqual((int)4, evento.CargaHoraria);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaEvento = _eventoService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaEvento, typeof(IEnumerable<Evento>));
            Assert.IsNotNull(listaEvento);
            Assert.AreEqual(3, listaEvento.Count());
            Assert.AreEqual((uint)1, listaEvento.First().Id);
        }
    }
}