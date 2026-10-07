using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoMapper;
using Core.Service;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using EventoWeb.Mappers;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using Core.DTO;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MySqlX.XDevAPI.Common;

namespace EventoWeb.Controllers.Tests
{
    [TestClass]
    public class ModelocrachaControllerTests
    {
        private static ModelocrachaController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IModelocrachaService>();
            var mockServiceEvento = new Mock<IEventoService>();
            var mockServicePessoa = new Mock<IPessoaService>();
            var mockServiceInscricao = new Mock<IInscricaoService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ModeloCrachaProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestModelocracha());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetModelocracha());
            mockService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns(GetTargetModelocracha());
            mockService.Setup(service => service.Create(It.IsAny<Modelocracha>()))
                .Verifiable();
            mockService.Setup(service => service.GetByEvento(It.IsAny<uint>()))
            .Returns(GetTestModelocracha());
            mockServiceInscricao.Setup(service => service.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 1, IdPapel = 2 });
            controller = new ModelocrachaController(mockService.Object, mockServiceEvento.Object, mockServicePessoa.Object, mockServiceInscricao.Object, mapper);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "12345678900"),
                new Claim(ClaimTypes.Role, "GESTOR")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")) }
            };
            controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
        }

        [TestMethod]
        public void IndexTest()
        {
            // Act
            var result = controller.Index(GetTargetEvento().Id, null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<ModelocrachaModel>));

            List<ModelocrachaModel>? lista = (List<ModelocrachaModel>)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Count);
        }

        [TestMethod]
        public void DetailsTest()
        {
            // Act
            var result = controller.Details(1, null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
            ModelocrachaModel modelocrachaModel = (ModelocrachaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, modelocrachaModel.Id);
            Assert.AreEqual((uint)1, modelocrachaModel.IdEvento);
            Assert.AreEqual("Texto 1", modelocrachaModel.Texto);
            Assert.AreEqual(1, modelocrachaModel.Qrcode);
        }

        [TestMethod]
        public void CreateTest()
        {
            // Act
            var result = controller.Create(GetTargetEvento().Id);

            // Assert 
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void CreateTest_Valid()
        {
            // Act
            var result = controller.Create(GetNewModelocracha());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
            Assert.AreEqual("Modelo salvo com sucesso", controller.TempData["SuccessMessage"]);
        }

        [TestMethod]
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Texto", "Informe o texto do crachá");

            // Act
            var result = controller.Create(GetNewModelocracha());

            // Assert
            Assert.AreEqual(1, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
        }

        [TestMethod]
        public void EditTest_Get_Valid()
        {
            // Act
            var result = controller.Edit(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
			ModelocrachaModel modelocrachaModel = (ModelocrachaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, modelocrachaModel.Id);
            Assert.AreEqual((uint)1, modelocrachaModel.IdEvento);
            Assert.AreEqual("Texto 1", modelocrachaModel.Texto);
            Assert.AreEqual(1, modelocrachaModel.Qrcode);
        }

        [TestMethod]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.Edit(GetTargetEditModelocrachaModel().Id, GetTargetEditModelocrachaModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
            Assert.AreEqual("Modelo salvo com sucesso", controller.TempData["SuccessMessage"]);
        }

        [TestMethod]
        public void DeleteTest_Post_Valid()
        {
            // Act
            var result = controller.Delete(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
            ModelocrachaModel modelocrachaModel = (ModelocrachaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, modelocrachaModel.Id);
            Assert.AreEqual((uint)1, modelocrachaModel.IdEvento);
        }

        [TestMethod]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetDeleteModelocrachaModel().Id, GetTargetDeleteModelocrachaModel().IdEvento);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        // IDOR (#764): gestor só gerencia o próprio evento.
        [TestMethod]
        public void Index_Details_EventoAlheio_Forbid()
        {
            var mockCracha = new Mock<IModelocrachaService>();
            mockCracha.Setup(s => s.GetByEvento(It.IsAny<uint>())).Returns(new List<Modelocracha>());
            mockCracha.Setup(s => s.Get(10)).Returns(new Modelocracha { Id = 10, IdEvento = 2 });
            mockCracha.Setup(s => s.Get(11)).Returns(new Modelocracha { Id = 11, IdEvento = 1 });
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new ModeloCrachaProfile())).CreateMapper();
            var mockInsc = new Mock<IInscricaoService>();
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), (uint)1))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 1, IdPapel = 2 });
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), (uint)2))
                .Returns((Inscricaopessoaevento)null!);
            var ctl = new ModelocrachaController(mockCracha.Object, new Mock<IEventoService>().Object, new Mock<IPessoaService>().Object, mockInsc.Object, mapper);
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GestorPrincipal() }
            };

            Assert.IsInstanceOfType(ctl.Index(2, null), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Index(1, null), typeof(ViewResult));
            Assert.IsInstanceOfType(ctl.Details(10, null), typeof(ForbidResult));
        }

        [TestMethod]
        public void CreateTest_Get_SemIdEvento()
        {
            // Act
            var result = controller.Create((uint?)null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
            var model = (ModelocrachaModel)viewResult.ViewData.Model;
            Assert.IsNotNull(model);
            Assert.AreEqual("Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.", model.Texto);
        }

        [TestMethod]
        public void EditTest_Post_SemNovoLogotipo_PreservaExistente()
        {
            // Arrange
            var modelEdit = GetTargetEditModelocrachaModel();
            modelEdit.Logotipo = null; // Sem subir nova imagem

            // Act
            var result = controller.Edit(modelEdit.Id, modelEdit);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod]
        public void IndexTest_SemIdEvento_RetornaLista()
        {
            // Act
            var result = controller.Index(null, null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<ModelocrachaModel>));
        }

        [TestMethod]
        public void ObterModeloPorEventoTest_ComModeloExistente_RetornaJson()
        {
            // Act
            var result = controller.ObterModeloPorEvento(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = (JsonResult)result;
            Assert.IsNotNull(jsonResult.Value);
        }

        [TestMethod]
        public void ObterModeloPorEventoTest_IdZero_RetornaNaoExiste()
        {
            // Act
            var resultZero = controller.ObterModeloPorEvento(0);

            // Assert
            Assert.IsInstanceOfType(resultZero, typeof(JsonResult));
        }

        [TestMethod]
        public void ObterModeloPorEventoTest_SemModelo_RetornaJsonPadrao()
        {
            // Arrange
            var mockCracha = new Mock<IModelocrachaService>();
            mockCracha.Setup(s => s.GetByEvento(99)).Returns(new List<Modelocracha>());
            var mockInsc = new Mock<IInscricaoService>();
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), 99))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 99, IdPapel = 2 });
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new ModeloCrachaProfile())).CreateMapper();
            var ctl = new ModelocrachaController(mockCracha.Object, new Mock<IEventoService>().Object, new Mock<IPessoaService>().Object, mockInsc.Object, mapper);
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GestorPrincipal() }
            };

            // Act
            var result = ctl.ObterModeloPorEvento(99);

            // Assert
            Assert.IsInstanceOfType(result, typeof(JsonResult));
        }

        [TestMethod]
        public void ObterModeloPorEventoTest_NaoAutorizado_RetornaForbid()
        {
            // Arrange
            var mockCracha = new Mock<IModelocrachaService>();
            var mockInsc = new Mock<IInscricaoService>();
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), 99))
                .Returns((Inscricaopessoaevento)null!);
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new ModeloCrachaProfile())).CreateMapper();
            var ctl = new ModelocrachaController(mockCracha.Object, new Mock<IEventoService>().Object, new Mock<IPessoaService>().Object, mockInsc.Object, mapper);
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GestorPrincipal() }
            };

            // Act
            var result = ctl.ObterModeloPorEvento(99);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ForbidResult));
        }

        [TestMethod]
        public void CreateTest_Post_QuandoModeloJaExisteParaEvento_Atualiza()
        {
            // Arrange
            var model = GetNewModelocracha();
            model.IdEvento = 1; // Evento 1 já possui modelo na mock setup

            // Act
            var result = controller.Create(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
        }

        [TestMethod]
        public void EditTest_Post_TrocaEventoAlvoSemModelo_EditaParaNovoEvento()
        {
            // Arrange
            var mockCracha = new Mock<IModelocrachaService>();
            var modeloExistente = new Modelocracha { Id = 1, IdEvento = 1, Texto = "Original", Qrcode = 1 };
            mockCracha.Setup(s => s.Get((uint)1)).Returns(modeloExistente);
            mockCracha.Setup(s => s.GetByEvento((uint)2)).Returns(new List<Modelocracha>());
            mockCracha.Setup(s => s.Edit(It.IsAny<Modelocracha>())).Verifiable();

            var mockInsc = new Mock<IInscricaoService>();
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), (uint)1))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 1, IdPapel = 2 });
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), (uint)2))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 2, IdPapel = 2 });

            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new ModeloCrachaProfile())).CreateMapper();
            var ctl = new ModelocrachaController(mockCracha.Object, new Mock<IEventoService>().Object, new Mock<IPessoaService>().Object, mockInsc.Object, mapper);
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GestorPrincipal() }
            };

            var formFileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(new byte[] { 0x1, 0x2 });
            formFileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            formFileMock.Setup(f => f.Length).Returns(ms.Length);
            formFileMock.Setup(f => f.CopyTo(It.IsAny<Stream>())).Callback<Stream>(s => ms.CopyTo(s));

            var viewModel = new ModelocrachaModel
            {
                Id = 1,
                IdEvento = 2, // Trocou para o Evento 2
                Texto = "Novo Texto para Evento 2",
                Qrcode = 1,
                Logotipo = formFileMock.Object
            };

            // Act
            var result = ctl.Edit(1, viewModel);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            mockCracha.Verify(s => s.Edit(It.Is<Modelocracha>(m => m.IdEvento == 2)), Times.Once);
        }

        [TestMethod]
        public void CreateTest_Post_SalvarRascunho_SemLogotipo_Sucesso()
        {
            // Arrange
            var model = GetNewModelocracha();
            model.IdEvento = 1;
            model.Logotipo = null; // Sem logotipo no rascunho
            model.LogotipoBase64 = null;

            // Act
            var result = controller.Create(model, "true");

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Rascunho salvo com sucesso", controller.TempData["SuccessMessage"]);
        }

        [TestMethod]
        public void EditTest_Post_SalvarRascunho_Sucesso()
        {
            // Arrange
            var modelEdit = GetTargetEditModelocrachaModel();
            modelEdit.Logotipo = null;

            // Act
            var result = controller.Edit(modelEdit.Id, modelEdit, "true");

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Rascunho salvo com sucesso", controller.TempData["SuccessMessage"]);
        }

        [TestMethod]
        public void CreateTest_Post_PermiteMultiplosModelosPorEvento_ChamaCreate()
        {
            // Arrange
            var mockCracha = new Mock<IModelocrachaService>();
            // Evento 1 já possui 3 modelos cadastrados
            mockCracha.Setup(s => s.GetByEvento(1)).Returns(GetTestModelocracha());
            mockCracha.Setup(s => s.Create(It.IsAny<Modelocracha>())).Returns(99);

            var mockInsc = new Mock<IInscricaoService>();
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), 1))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = 1, IdPapel = 2 });

            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new ModeloCrachaProfile())).CreateMapper();
            var ctl = new ModelocrachaController(mockCracha.Object, new Mock<IEventoService>().Object, new Mock<IPessoaService>().Object, mockInsc.Object, mapper);
            ctl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GestorPrincipal() }
            };
            ctl.TempData = new TempDataDictionary(ctl.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var novoModelo = GetNewModelocracha();
            novoModelo.IdEvento = 1;
            novoModelo.Texto = "Segundo Modelo para o Evento 1";

            // Act
            var result = ctl.Create(novoModelo);

            // Assert: deve criar um novo modelo chamando Create, e nunca sobrescrever chamando Edit
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            mockCracha.Verify(s => s.Create(It.Is<Modelocracha>(m => m.IdEvento == 1 && m.Texto == "Segundo Modelo para o Evento 1")), Times.Once);
            mockCracha.Verify(s => s.Edit(It.IsAny<Modelocracha>()), Times.Never);
        }

        private static ClaimsPrincipal GestorPrincipal() => new(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.Name, "12345678900"),
            new Claim(ClaimTypes.Role, "GESTOR")
        }, "TestAuthType"));

        private ModelocrachaModel GetNewModelocracha()
        {
            var formFileMock = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "test.png";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            formFileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            formFileMock.Setup(_ => _.FileName).Returns(fileName);
            formFileMock.Setup(_ => _.Length).Returns(ms.Length);

			var evento = new EventoSimpleDTO
			{
				Id = 1,
				Nome = "SEMINFO"
			};

			return new ModelocrachaModel
			{
				Id = 7,
                IdEvento = 4,
                Logotipo = formFileMock.Object,
                Texto = "Texto 4",
                Qrcode = 1,
                Evento = evento
            };
        }

        private static Modelocracha GetTargetModelocracha()
        {
            return new Modelocracha
            {
                Id = 1,
                IdEvento = 1,
                Logotipo = new byte[] { 0x20, 0x20 },
                Texto = "Texto 1",
                Qrcode = 1
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

        private ModelocrachaModel GetTargetEditModelocrachaModel()
        {
            var formFileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(new byte[] { 0x20, 0x20 });
            formFileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            formFileMock.Setup(_ => _.Length).Returns(ms.Length);

			var evento = new EventoSimpleDTO
			{
				Id = 1,
				Nome = "SEMINFO"
			};

			return new ModelocrachaModel
			{
				Id = 7,
				IdEvento = 4,
				Logotipo = formFileMock.Object,
				Texto = "Texto 4",
				Qrcode = 1,
				Evento = evento
			};
	}

        private ModelocrachaModel GetTargetDeleteModelocrachaModel()
        {
            var formFileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(new byte[] { 0x20, 0x20 });
            formFileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            formFileMock.Setup(_ => _.Length).Returns(ms.Length);

            return new ModelocrachaModel
            {
                Id = 1,
                IdEvento = 1,
                Logotipo = formFileMock.Object,
                Texto = "Texto 1",
                Qrcode = 1
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
                }
            };
        }

        private IEnumerable<Modelocracha> GetTestModelocracha()
        {
            return new List<Modelocracha>
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
                    IdEvento = 1,
                    Logotipo = new byte[] { 0x30, 0x30 },
                    Texto = "Texto 2",
                    Qrcode = 1
                },
                new Modelocracha
                {
                    Id = 5,
                    IdEvento = 1,
                    Logotipo = new byte[] { 0x50, 0x50 },
                    Texto = "Texto 3",
                    Qrcode = 1
                },
            };
        }
    }
}