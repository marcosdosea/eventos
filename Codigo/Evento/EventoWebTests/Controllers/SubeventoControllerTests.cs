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
    public class SubeventoControllerTests
    {
        private static SubeventoController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<ISubeventoService>();
            var mockServiceEvento = new Mock<IEventoService>();
            var mockServiceTipoevento = new Mock<ITipoeventoService>();
            var mockServiceTipoInscricao = new Mock<ITipoInscricaoService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new SubeventoProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                            .Returns(GetTestSubeventos());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetSubevento());
            mockService.Setup(service => service.Create(It.IsAny<Subevento>()))
                .Verifiable();
            mockServiceEvento.Setup(service => service.GetNomeById(1))
                .Returns("SEMINFO");
            mockServiceTipoevento.Setup(service => service.GetNomeById(1))
                .Returns("Tecnologia");

            controller = new SubeventoController(mockService.Object, mapper, mockServiceEvento.Object, mockServiceTipoevento.Object, mockServiceTipoInscricao.Object);
        }

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<SubeventoModel>));

            List<SubeventoModel>? lista = (List<SubeventoModel>)viewResult.ViewData.Model;
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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel subeventoModel = (SubeventoModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, subeventoModel.IdEvento);
            Assert.AreEqual("SEMINFO", subeventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", subeventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subeventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subeventoModel.DataFim);
            Assert.AreEqual((sbyte)1, subeventoModel.InscricaoGratuita);
            Assert.AreEqual("A", subeventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subeventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subeventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0, subeventoModel.ValorInscricao);
            Assert.AreEqual((sbyte)1, subeventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1, subeventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, subeventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, subeventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, subeventoModel.VagasReservadas);
            Assert.AreEqual((int)65, subeventoModel.VagasDisponiveis);
            Assert.AreEqual((int)4, subeventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            var result = controller.CreateOrEdit(1,null);

            // Assert 
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
        }

        [TestMethod()]
        public void CreateTest_Valid()
        {
            // Act
            var result = controller.CreateOrEdit(1,null,GetNewSubevento());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("GerenciarEvento", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Nome", "Nome do Subevento é obrigatório");

            // Act
            var result = controller.CreateOrEdit(1, null, GetNewSubevento());

			// Assert
			Assert.AreEqual(1, controller.ModelState.ErrorCount);
			Assert.IsInstanceOfType(result, typeof(ViewResult));
			ViewResult viewResult = (ViewResult)result;
			Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
		}


        [TestMethod()]
        public void EditTest_Get_Valid()
        {
            // Act
            var result = controller.CreateOrEdit(1,1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
			SubeventoModel subeventoModel = (SubeventoModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, subeventoModel.IdEvento);
            Assert.AreEqual("SEMINFO", subeventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", subeventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subeventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subeventoModel.DataFim);
            Assert.AreEqual((sbyte)1, subeventoModel.InscricaoGratuita);
            Assert.AreEqual("A", subeventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subeventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subeventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0, subeventoModel.ValorInscricao);
            Assert.AreEqual((sbyte)1, subeventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1, subeventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, subeventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, subeventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, subeventoModel.VagasReservadas);
            Assert.AreEqual((int)65, subeventoModel.VagasDisponiveis);
            Assert.AreEqual((int)4, subeventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.CreateOrEdit(GetTargetSubeventoModel().IdEvento, GetTargetSubeventoModel().Id,GetNewSubevento());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("GerenciarEvento", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel subeventoModel = (SubeventoModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, subeventoModel.IdEvento);
            Assert.AreEqual("SEMINFO", subeventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", subeventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subeventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subeventoModel.DataFimInscricao);
            Assert.AreEqual((sbyte)1, subeventoModel.InscricaoGratuita);
            Assert.AreEqual("A", subeventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), subeventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), subeventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0, subeventoModel.ValorInscricao);
            Assert.AreEqual((sbyte)1, subeventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1, subeventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, subeventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, subeventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, subeventoModel.VagasReservadas);
            Assert.AreEqual((int)65, subeventoModel.VagasDisponiveis);
            Assert.AreEqual((int)4, subeventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void DeleteTest_Post_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetSubeventoModel().Id, GetTargetSubeventoModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        private SubeventoModel GetNewSubevento()
        {
			return new SubeventoModel
			{
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
            };
        }
        private static Subevento GetTargetSubevento()
        {
            return new Subevento
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
            };
        }

        private SubeventoModel GetTargetSubeventoModel()
        {
            return new SubeventoModel
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

        private IEnumerable<Subevento> GetTestSubeventos()
        {
            return new List<Subevento>
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
                        IdEvento = 1,
                        Nome = "SEMAC",
                        Descricao = "Semana academica de cursos",
                        DataInicio = new DateTime(2024, 02, 2, 7, 30, 0),
                        DataFim = new DateTime(2024, 02, 7, 12, 30, 0),
                        InscricaoGratuita = 1,
                        Status = "F",
                        DataInicioInscricao = new DateTime(2024, 02, 2, 7, 30, 0),
                        DataFimInscricao = new DateTime(2024, 02, 7, 12, 30, 0),
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
                        IdEvento = 1,
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
        }
    }
}