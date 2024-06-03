using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class EventoControllerTests
    {
        private static EventoController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IEventoService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new EventoProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestEventos());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetEvento());
            mockService.Setup(service => service.Create(It.IsAny<Evento>()))
                .Verifiable();
            controller = new EventoController(mockService.Object, mapper);
        }

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<EventoModel>));

            List<EventoModel>? lista = (List<EventoModel>)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Count);
        }

        [TestMethod()]
        public void DetailsTest()
        {
            // Act
            var result = controller.Details(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
            EventoModel eventoModel = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Cep);
            Assert.AreEqual("SE", eventoModel.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Cidade);
            Assert.AreEqual("Porto", eventoModel.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Rua);
            Assert.AreEqual("s/n", eventoModel.Numero);
            Assert.AreEqual("Universidade", eventoModel.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((int)1, eventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            var result = controller.Create();

            // Assert 
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod()]
        public void CreateTest_Valid()
        {
            // Act
            var result = controller.Create(GetNewEvento());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Nome", "Nome do Evento é obrigatório");

            // Act
            var result = controller.Create(GetNewEvento());

            // Assert
            Assert.AreEqual(1, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }


        [TestMethod()]
        public void EditTest_Get_Valid()
        {
            // Act
            var result = controller.Edit(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
            EventoModel eventoModel = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Cep);
            Assert.AreEqual("SE", eventoModel.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Cidade);
            Assert.AreEqual("Porto", eventoModel.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Rua);
            Assert.AreEqual("s/n", eventoModel.Numero);
            Assert.AreEqual("Universidade", eventoModel.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((int)1, eventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.Edit(GetTargetEventoModel().Id, GetTargetEventoModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void DeleteTest_Post_Valid()
        {
            // Act
            var result = controller.Delete(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
            EventoModel eventoModel = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Cep);
            Assert.AreEqual("SE", eventoModel.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Cidade);
            Assert.AreEqual("Porto", eventoModel.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Rua);
            Assert.AreEqual("s/n", eventoModel.Numero);
            Assert.AreEqual("Universidade", eventoModel.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((int)1, eventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetEventoModel().Id, GetTargetEventoModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        private EventoModel GetNewEvento()
        {
            return new EventoModel
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
            };
        }
        private static Evento GetTargetEvento()
        {
            return new Evento
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
            };
        }

        private EventoModel GetTargetEventoModel()
        {
            return new EventoModel
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
            };
        }

        private IEnumerable<Evento> GetTestEventos()
        {
            return new List<Evento>
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
                        Id = 3,
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
                        Id = 5,
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
        }
    }
}