using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;
namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class SubeventoControllerTests
    {
        private static SubeventoController controller = null!;

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

            string cpfTeste = "12345678900";
            uint papelGestor = 2;

            var listaEventosTeste = new List<Evento>
            {
                new Evento
                {
                    Id = 1,
                    Nome = "SEMINFO",
                    DataInicio = new DateTime(2024, 09, 1, 0, 0, 0),
                    DataFim = new DateTime(2024, 09, 10, 0, 0, 0)
                }
            };

            mockServiceEvento.Setup(service => service.GetEventByCpf(cpfTeste, papelGestor))
                .Returns(listaEventosTeste);

            mockServiceEvento.Setup(service => service.GetAll())
                .Returns(listaEventosTeste);

            mockServiceEvento.Setup(service => service.Get(1))
                .Returns(listaEventosTeste[0]);

            mockServiceEvento.Setup(service => service.GetEventoSimpleDto(1))
                .Returns(new EventoSimpleDTO { Id = 1, Nome = "SEMINFO" });

            mockServiceEvento.Setup(service => service.GetNomeById(1))
                .Returns("SEMINFO");

            mockServiceTipoevento.Setup(service => service.GetAll())
                .Returns(new List<Tipoevento> { new Tipoevento { Id = 1, Nome = "Palestra" } });

            mockServiceTipoInscricao.Setup(service => service.GetTiposInscricaosSubevento(1))
                .Returns(new List<TipoInscricaoDTO>());

            var mockServiceInscricao = new Mock<IInscricaoService>();
            mockServiceInscricao.Setup(service => service.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 1, IdPapel = 2 });

            controller = new SubeventoController(mockService.Object, mapper, mockServiceEvento.Object, mockServiceTipoevento.Object, mockServiceTipoInscricao.Object, mockServiceInscricao.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, cpfTeste),
                new Claim(ClaimTypes.Role, "GESTOR")
             };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Injeta o usuário fictício dentro do contexto do Controller
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
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
            var result = controller.CreateOrEdit(1, (uint?)null);

            // Assert 
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel subeventoModel = (SubeventoModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, subeventoModel.IdEvento);
        }

        [TestMethod()]
        public void CreateTest_Valid()
        {
            // Act
            var result = controller.CreateOrEdit(1, GetNewSubevento());

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
            var result = controller.CreateOrEdit(1, GetNewSubevento());

            // Assert
            Assert.AreEqual(1, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel subeventoModel = (SubeventoModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, subeventoModel.IdEvento);
        }

        [TestMethod()]
        public void CreateTest_GratuitoComValor_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.InscricaoGratuita = 1;
            subevento.ValorInscricao = 10.00m;

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("ValorInscricao"));
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
        }

        [TestMethod()]
        public void CreateTest_DataInicioAntesDoEventoPai_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.DataInicio = new DateTime(2024, 08, 31, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataInicio"));
            Assert.AreEqual("A data de início do subevento não pode ser anterior ao início do evento principal.",
                controller.ModelState["DataInicio"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void CreateTest_DataFimAposEventoPai_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.DataFim = new DateTime(2024, 09, 15, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataFim"));
            Assert.AreEqual("A data de término do subevento não pode ser posterior ao término do evento principal.",
                controller.ModelState["DataFim"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void CreateTest_DataInicioInscricaoAposDataFimInscricao_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.DataInicioInscricao = new DateTime(2024, 09, 1, 0, 0, 0);
            subevento.DataFimInscricao = new DateTime(2024, 08, 20, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataInicioInscricao"));
            Assert.AreEqual("A data inicial de inscrição não pode ser posterior à data final de inscrição.",
                controller.ModelState["DataInicioInscricao"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void CreateTest_DataFimInscricaoAposInicioSubevento_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.DataFimInscricao = subevento.DataInicio.AddHours(1);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataFimInscricao"));
            Assert.AreEqual("O período de inscrições deve encerrar antes ou no início do subevento.",
                controller.ModelState["DataFimInscricao"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void CreateTest_PeriodoInscricaoNoLimiteInicioSubevento_Valid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.DataFimInscricao = subevento.DataInicio;

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.IsValid);
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("GerenciarEvento", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void CreateTest_DatasLimitesDoEventoPai_Valid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.DataInicio = new DateTime(2024, 09, 1, 0, 0, 0);
            subevento.DataFim = new DateTime(2024, 09, 10, 0, 0, 0);
            subevento.DataInicioInscricao = new DateTime(2024, 08, 20, 0, 0, 0);
            subevento.DataFimInscricao = new DateTime(2024, 09, 1, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.IsValid);
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("GerenciarEvento", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void CreateTest_MultiplasDatasInvalidas_RegistraTodosErros()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.DataInicio = new DateTime(2024, 08, 30, 0, 0, 0);
            subevento.DataFim = new DateTime(2024, 09, 15, 0, 0, 0);
            subevento.DataInicioInscricao = new DateTime(2024, 09, 5, 0, 0, 0);
            subevento.DataFimInscricao = new DateTime(2024, 09, 2, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsFalse(controller.ModelState.IsValid);
            Assert.AreEqual(4, controller.ModelState.ErrorCount);
            Assert.IsTrue(controller.ModelState.ContainsKey("DataInicio"));
            Assert.AreEqual("A data de início do subevento não pode ser anterior ao início do evento principal.",
                controller.ModelState["DataInicio"]!.Errors[0].ErrorMessage);
            Assert.IsTrue(controller.ModelState.ContainsKey("DataFim"));
            Assert.AreEqual("A data de término do subevento não pode ser posterior ao término do evento principal.",
                controller.ModelState["DataFim"]!.Errors[0].ErrorMessage);
            Assert.IsTrue(controller.ModelState.ContainsKey("DataInicioInscricao"));
            Assert.AreEqual("A data inicial de inscrição não pode ser posterior à data final de inscrição.",
                controller.ModelState["DataInicioInscricao"]!.Errors[0].ErrorMessage);
            Assert.IsTrue(controller.ModelState.ContainsKey("DataFimInscricao"));
            Assert.AreEqual("O período de inscrições deve encerrar antes ou no início do subevento.",
                controller.ModelState["DataFimInscricao"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void EditTest_Post_DataInicioAntesDoEventoPai_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.Id = 1;
            subevento.DataInicio = new DateTime(2024, 08, 31, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataInicio"));
            Assert.AreEqual("A data de início do subevento não pode ser anterior ao início do evento principal.",
                controller.ModelState["DataInicio"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void EditTest_Post_DataFimAposEventoPai_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.Id = 1;
            subevento.DataFim = new DateTime(2024, 09, 15, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataFim"));
            Assert.AreEqual("A data de término do subevento não pode ser posterior ao término do evento principal.",
                controller.ModelState["DataFim"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void EditTest_Post_DataInicioInscricaoAposDataFimInscricao_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.Id = 1;
            subevento.DataInicioInscricao = new DateTime(2024, 09, 1, 0, 0, 0);
            subevento.DataFimInscricao = new DateTime(2024, 08, 20, 0, 0, 0);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataInicioInscricao"));
            Assert.AreEqual("A data inicial de inscrição não pode ser posterior à data final de inscrição.",
                controller.ModelState["DataInicioInscricao"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }

        [TestMethod()]
        public void EditTest_Post_DataFimInscricaoAposInicioSubevento_Invalid()
        {
            // Arrange
            var subevento = GetNewSubevento();
            subevento.Id = 1;
            subevento.DataFimInscricao = subevento.DataInicio.AddHours(1);

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(controller.ModelState.ContainsKey("DataFimInscricao"));
            Assert.AreEqual("O período de inscrições deve encerrar antes ou no início do subevento.",
                controller.ModelState["DataFimInscricao"]!.Errors[0].ErrorMessage);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(SubeventoModel));
            SubeventoModel model = (SubeventoModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model.TiposEventos);
            Assert.IsNotNull(model.Evento);
        }


        [TestMethod()]
        public void EditTest_Get_Valid()
        {
            // Act
            var result = controller.CreateOrEdit(1, 1);

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
            // Arrange
            var subevento = GetNewSubevento();
            subevento.Id = 1;

            // Act
            var result = controller.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("GerenciarEvento", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void CreateTest_EventoPaiComDatasNulas_Valid()
        {
            // Arrange
            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1)).Returns(new Evento { Id = 1, Nome = "Evento Sem Datas", DataInicio = null, DataFim = null });
            mockEventoService.Setup(s => s.GetEventoSimpleDto(1)).Returns(new EventoSimpleDTO { Id = 1, Nome = "Evento Sem Datas" });

            var mockSubService = new Mock<ISubeventoService>();
            var mockTipoEventoService = new Mock<ITipoeventoService>();
            mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento> { new Tipoevento { Id = 1, Nome = "Palestra" } });
            var mockTipoInscricaoService = new Mock<ITipoInscricaoService>();

            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new SubeventoProfile())).CreateMapper();

            var ctl = new SubeventoController(
                mockSubService.Object,
                mapper,
                mockEventoService.Object,
                mockTipoEventoService.Object,
                mockTipoInscricaoService.Object,
                MockInscricaoSozinhoEventoProprio().Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "12345678900"),
                new Claim(ClaimTypes.Role, "GESTOR")
            };
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            var subevento = GetNewSubevento();

            // Act
            var result = ctl.CreateOrEdit(1, subevento);

            // Assert
            Assert.IsTrue(ctl.ModelState.IsValid);
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
            var result = controller.Delete(1, GetTargetSubeventoModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        // IDOR (#764): gestor só gerencia o próprio evento.
        [TestMethod()]
        public void Details_EventoAlheio_Forbid_Proprio_Ok()
        {
            var mockSub = new Mock<ISubeventoService>();
            mockSub.Setup(s => s.Get(10)).Returns(new Subevento { Id = 10, IdEvento = 2, Nome = "A" });
            mockSub.Setup(s => s.Get(11)).Returns(new Subevento { Id = 11, IdEvento = 1, Nome = "B" });
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new SubeventoProfile())).CreateMapper();
            var ctl = new SubeventoController(mockSub.Object, mapper, new Mock<IEventoService>().Object, new Mock<ITipoeventoService>().Object, new Mock<ITipoInscricaoService>().Object, MockInscricaoSozinhoEventoProprio().Object);
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GestorPrincipal() }
            };

            Assert.IsInstanceOfType(ctl.Details(10), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Details(11), typeof(ViewResult));
        }

        // IDOR (#764)
        [TestMethod()]
        public void CreateOrEdit_EventoAlheio_Forbid()
        {
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new SubeventoProfile())).CreateMapper();
            var ctl = new SubeventoController(new Mock<ISubeventoService>().Object, mapper, new Mock<IEventoService>().Object, new Mock<ITipoeventoService>().Object, new Mock<ITipoInscricaoService>().Object, MockInscricaoSozinhoEventoProprio().Object);
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GestorPrincipal() }
            };

            Assert.IsInstanceOfType(ctl.CreateOrEdit(2, (uint?)null), typeof(ForbidResult));
        }

        private static ClaimsPrincipal GestorPrincipal() => new(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.Name, "12345678900"),
            new Claim(ClaimTypes.Role, "GESTOR")
        }, "TestAuthType"));

        private static Mock<IInscricaoService> MockInscricaoSozinhoEventoProprio()
        {
            var mock = new Mock<IInscricaoService>();
            mock.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), (uint)1))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 1, IdPapel = 2 });
            mock.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), (uint)2))
                .Returns((Inscricaopessoaevento)null!);
            return mock;
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
                DataInicioInscricao = new DateTime(2024, 08, 20, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 09, 1, 12, 30, 0),
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
                    Id = 2,
                    Nome = "ENCONTRO",
                    Descricao = "Evento para encontro de estudantes",
                    DataInicio = new DateTime(2024, 11, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 11, 7, 12, 30, 0),
                    InscricaoGratuita = 0,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 10, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 10, 7, 12, 30, 0),
                    ValorInscricao = 50,
                    Website = "www.encontro.com.br",
                    EmailEvento = "encontro@gmail.com",
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
                    VagasOfertadas = 200,
                    VagasReservadas = 50,
                    VagasDisponiveis = 150,
                    TempoMinutosReserva = 240,
                    CargaHoraria = 8,
                },
                new Evento
                {
                    Id = 3,
                    Nome = "CONGRESSO",
                    Descricao = "Evento para congresso de tecnologia",
                    DataInicio = new DateTime(2024, 12, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 12, 7, 12, 30, 0),
                    InscricaoGratuita = 0,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 11, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 11, 7, 12, 30, 0),
                    ValorInscricao = 100,
                    Website = "www.congresso.com.br",
                    EmailEvento = "congresso@gmail.com",
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
                    VagasOfertadas = 300,
                    VagasReservadas = 75,
                    VagasDisponiveis = 225,
                    TempoMinutosReserva = 240,
                    CargaHoraria = 12,
                }
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
                    Id = 2,
                    IdEvento = 1,
                    Nome = "ENCONTRO",
                    Descricao = "Evento para encontro de estudantes",
                    DataInicio = new DateTime(2024, 11, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 11, 7, 12, 30, 0),
                    InscricaoGratuita = 0,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 10, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 10, 7, 12, 30, 0),
                    ValorInscricao = 50,
                    PossuiCertificado = 1,
                    FrequenciaMinimaCertificado = 1,
                    IdTipoEvento = 1,
                    VagasOfertadas = 200,
                    VagasReservadas = 50,
                    VagasDisponiveis = 150,
                    CargaHoraria = 8,
                },
                new Subevento
                {
                    Id = 3,
                    IdEvento = 1,
                    Nome = "CONGRESSO",
                    Descricao = "Evento para congresso de tecnologia",
                    DataInicio = new DateTime(2024, 12, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 12, 7, 12, 30, 0),
                    InscricaoGratuita = 0,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 11, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 11, 7, 12, 30, 0),
                    ValorInscricao = 100,
                    PossuiCertificado = 1,
                    FrequenciaMinimaCertificado = 1,
                    IdTipoEvento = 1,
                    VagasOfertadas = 300,
                    VagasReservadas = 75,
                    VagasDisponiveis = 225,
                    CargaHoraria = 12,
                }
            };
        }
    }
}