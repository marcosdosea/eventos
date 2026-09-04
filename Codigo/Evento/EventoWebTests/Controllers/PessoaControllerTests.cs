using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace EventoWeb.Controllers.Tests
{
    public class TestUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; set; } = new ActionContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost")
                }
            },
            RouteData = new Microsoft.AspNetCore.Routing.RouteData()
        };

        public string? Action(UrlActionContext actionContext)
        {
            return null;
        }

        public string? Content(string? contentPath) => contentPath;

        public bool IsLocalUrl(string? url) => true;

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            return "https://localhost/Identity/Account/ResetPassword?code=test-token";
        }

        public string? Action(string actionName, string? controllerName = null, object? values = null, string? protocol = null, string? host = null, string? fragment = null) => null;

        public string? Link(string? routeName, object? values = null) => null;
    }

    [TestClass()]
    public class PessoaControllerTests
    {
        private static PessoaController? controller;

        [TestInitialize]
        public void Initialize()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosbrasilService = new Mock<IEstadosbrasilService>();
            var IEmailServiceMock = new Mock<IEmailService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new PessoaProfile())).CreateMapper();

            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestPessoas());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetPessoa());
            mockService.Setup(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            mockService.Setup(service => service.Delete(It.IsAny<uint>()));
            mockService.Setup(service => service.CreatePessoaIdentityComPapelAsync(GetTargetPessoa(),1,1));
            mockService.Setup(service => service.Edit(It.IsAny<Pessoa>()))
                .Returns(Task.CompletedTask);
            controller = new PessoaController(mockService.Object, mockEstadosbrasilService.Object, mapper, IEmailServiceMock.Object);
        }

        [TestMethod()]
        public void IndexTest()
        {
            var result = controller!.Index();

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<PessoaModel>));

            List<PessoaModel>? lista = (List<PessoaModel>)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Count);
        }

        [TestMethod()]
        public void DetailsTest()
        {
            var result = controller!.Details(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(PessoaModel));
            PessoaModel pessoaModel = (PessoaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, pessoaModel.Id);
            Assert.AreEqual("João Vitor Sodré", pessoaModel.Nome);
            Assert.AreEqual("Sodré", pessoaModel.NomeCracha);
            Assert.AreEqual("040.268.930-57", pessoaModel.Cpf);
            Assert.AreEqual("M", pessoaModel.Sexo);
            Assert.AreEqual("48370-000", pessoaModel.Cep);
            Assert.AreEqual("Avenida Principal", pessoaModel.Rua);
            Assert.AreEqual("Centro", pessoaModel.Bairro);
            Assert.AreEqual("Irece", pessoaModel.Cidade);
            Assert.AreEqual("BA", pessoaModel.Estado);
            Assert.AreEqual("s/n", pessoaModel.Numero);
            Assert.AreEqual("casa", pessoaModel.Complemento);
            Assert.AreEqual("email@gmail.com", pessoaModel.Email);
            Assert.AreEqual("7999990011", pessoaModel.Telefone1);
            Assert.AreEqual("NULL", pessoaModel.Telefone2);
        }

        [TestMethod()]
        public void CreateTest()
        {
            string? returnUrl = null;
            var result = controller!.Create(returnUrl);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod()]
        public async Task CreateTest_Valid()
        {
            string? returnUrl = null;
            controller!.ModelState.Clear();
            var mockService = new Mock<IPessoaService>();
            mockService.Setup(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            var IEmailServiceMock = new Mock<IEmailService>();
            controller = new PessoaController(mockService.Object, new Mock<IEstadosbrasilService>().Object,
            new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper(), IEmailServiceMock.Object);

            var result = await controller.Create(GetNewPessoa(), returnUrl);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
           
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()), Times.Once);
        }

        [TestMethod()]
        public async Task CreateTest_Invalid()
        {
            string? returnUrl = null;
            controller!.ModelState.Clear();
            var mockService = new Mock<IPessoaService>();
            var IEmailServiceMock = new Mock<IEmailService>();

            controller = new PessoaController(mockService.Object, new Mock<IEstadosbrasilService>().Object,
            new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper(), IEmailServiceMock.Object);

            controller.ModelState.AddModelError("Nome", "Campo requerido");
            controller.ModelState.AddModelError("NomeCracha", "Campo requerido");
            controller.ModelState.AddModelError("Cpf", "Campo requerido");
            controller.ModelState.AddModelError("Sexo", "Campo requerido");
            controller.ModelState.AddModelError("Email", "Campo requerido");

            var result = await controller.Create(GetNewPessoa(), returnUrl);

            Assert.AreEqual(5, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(PessoaModel));
        }

        [TestMethod()]
        public void EditTest_Get_Valid()
        {
            string? returnUrl = null;
            var pessoa = GetTargetPessoa();

            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.Get(pessoa.Id)).Returns(pessoa);
            var IEmailServiceMock = new Mock<IEmailService>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailServiceMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, pessoa.Cpf),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            var result = localController.Edit(1, returnUrl);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(PessoaModel));
            PessoaModel pessoaModel = (PessoaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, pessoaModel.Id);
        }

        [TestMethod()]
        public async Task EditTest_Post_Valid()
        {
            string? returnUrl = null;
            var model = GetTargetPessoaModel();
            var pessoa = GetTargetPessoa();

            var mockService = new Mock<IPessoaService>();
            mockService.Setup(service => service.Get(model.Id)).Returns(pessoa);
            mockService.Setup(service => service.Edit(It.IsAny<Pessoa>())).Verifiable();
            var IEmailServiceMock = new Mock<IEmailService>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, new Mock<IEstadosbrasilService>().Object, mapper, IEmailServiceMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, model.Cpf),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            localController.ModelState.Clear();
            
            var result = await localController.Edit(model.Id, model, returnUrl);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
            
            mockService.Verify(service => service.Edit(It.IsAny<Pessoa>()), Times.Once);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            string? returnUrl = null;
            var result = controller!.Delete(GetTargetPessoaModel());

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(PessoaModel));
            PessoaModel pessoaModel = (PessoaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, pessoaModel.Id);
            Assert.AreEqual("João Vitor Sodré", pessoaModel.Nome);
            Assert.AreEqual("Sodré", pessoaModel.NomeCracha);
            Assert.AreEqual("040.268.930-57", pessoaModel.Cpf);
            Assert.AreEqual("M", pessoaModel.Sexo);
            Assert.AreEqual("48370-000", pessoaModel.Cep);
            Assert.AreEqual("Avenida Principal", pessoaModel.Rua);
            Assert.AreEqual("Centro", pessoaModel.Bairro);
            Assert.AreEqual("Irece", pessoaModel.Cidade);
            Assert.AreEqual("BA", pessoaModel.Estado);
            Assert.AreEqual("s/n", pessoaModel.Numero);
            Assert.AreEqual("casa", pessoaModel.Complemento);
            Assert.AreEqual("email@gmail.com", pessoaModel.Email);
            Assert.AreEqual("7999990011", pessoaModel.Telefone1);
            Assert.AreEqual("NULL", pessoaModel.Telefone2);
        }

        [TestMethod()]
        public async Task DeleteTest_Post_Valid()
        {
                            
            controller!.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            var result = await controller!.DeleteConfirmed(GetTargetPessoaModel());
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
            Assert.AreEqual("Pessoa", redirectToActionResult.ControllerName);
            Assert.AreEqual("Erro ao excluir pessoa", controller.TempData["ErrorMessage"]);
            Assert.IsNull(controller.TempData["SuccessMessage"]);
        }


        [TestMethod()]
        public async Task DeleteAdmTest_Post_Valid()
        {
            var pessoa = GetTargetPessoa();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, pessoa.Cpf),
        new Claim(ClaimTypes.Role, "ADMINISTRADOR")
    }, "mock"));

            controller!.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            var result = await controller!.DeleteConfirmed(GetTargetPessoaModel());
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectToActionResult.ActionName);
            Assert.AreEqual("Pessoa", redirectToActionResult.ControllerName);
            Assert.AreEqual("Erro ao excluir pessoa", controller.TempData["ErrorMessage"]);
            Assert.IsNull(controller.TempData["SuccessMessage"]);
        }

        [TestMethod()]
        public async Task DeleteAdmTest_Post_Success()
        {
            var mockService = new Mock<IPessoaService>();
            mockService.Setup(service => service.Delete(1)).ReturnsAsync(true);
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var localController = new PessoaController(mockService.Object, new Mock<IEstadosbrasilService>().Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "040.268.930-57"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());

            var result = await localController.DeleteConfirmed(GetTargetPessoaModel());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectToActionResult.ActionName);
            Assert.AreEqual("Pessoa", redirectToActionResult.ControllerName);
            Assert.AreEqual("Exclusão realizada com sucesso!", localController.TempData["SuccessMessage"]);
            Assert.IsNull(localController.TempData["ErrorMessage"]);
            mockService.Verify(service => service.Delete(1), Times.Once);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Get_Valid()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            var admins = new List<Pessoa>
            {
                GetTargetPessoa(),
                new Pessoa
                {
                    Id = 2,
                    Nome = "Maria Silva",
                    NomeCracha = "Maria",
                    Cpf = "123.456.789-00",
                    Sexo = "F",
                    Cep = "48370-000",
                    Rua = "Rua Teste",
                    Bairro = "Centro",
                    Cidade = "Irece",
                    Estado = "BA",
                    Numero = "100",
                    Complemento = "apto",
                    Email = "maria@gmail.com",
                    Telefone1 = "7999990022",
                    Telefone2 = "NULL"
                }
            };
            mockService.Setup(service => service.GetAllAdmAsync()).ReturnsAsync(admins);
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "040.268.930-57"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            var result = await localController.DefinirAdministrador();

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoAdministradorModel));
            GestaoAdministradorModel model = (GestaoAdministradorModel)viewResult.ViewData.Model;
            Assert.AreEqual(2, model.Administradores.Count);

            // Ordenação por nome aplicada pelo controller: João < Maria
            Assert.IsTrue(string.Compare(model.Administradores[0].Nome, model.Administradores[1].Nome, StringComparison.Ordinal) < 0);

            // Additional assertions - verify admin properties are mapped correctly
            Assert.AreEqual((uint)1, model.Administradores[0].Id);
            Assert.AreEqual("João Vitor Sodré", model.Administradores[0].Nome);
            Assert.AreEqual("040.268.930-57", model.Administradores[0].Cpf);
            Assert.AreEqual("email@gmail.com", model.Administradores[0].Email);
            
            Assert.AreEqual((uint)2, model.Administradores[1].Id);
            Assert.AreEqual("Maria Silva", model.Administradores[1].Nome);
            Assert.AreEqual("123.456.789-00", model.Administradores[1].Cpf);
            Assert.AreEqual("maria@gmail.com", model.Administradores[1].Email);
            
            mockService.Verify(service => service.GetAllAdmAsync(), Times.Once);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Post_Valid()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.ValidaEmail(It.IsAny<string>())).Returns(true);
            mockService.Setup(service => service.EmailExist(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            mockService.Setup(service => service.IsAdmAsync(It.IsAny<Pessoa>())).ReturnsAsync(false);
            mockService.Setup(service => service.VerificaEdit(It.IsAny<Pessoa>())).ReturnsAsync(true);
            mockService.Setup(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), 0, 1)).ReturnsAsync(true);
            mockService.Setup(service => service.GetAllAdmAsync()).ReturnsAsync(new List<Pessoa> { GetTargetPessoa() });
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "040.268.930-57"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());
            localController.ModelState.Clear();

            var viewModel = new GestaoAdministradorModel
            {
                Id = 4,
                Nome = "Novo Admin",
                Cpf = "999.999.999-99",
                Email = "novo@admin.com",
                Telefone1 = "7999999999"
            };

            var result = await localController.DefinirAdministrador(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.IsNull(redirectResult.ControllerName);

            // Additional assertions - verify success message and service calls
            Assert.AreEqual("Administrador definido com sucesso.", localController.TempData["SuccessMessage"]);
            Assert.IsNull(localController.TempData["ErrorMessage"]);
            mockService.Verify(service => service.ValidaEmail("novo@admin.com"), Times.Once);
            mockService.Verify(service => service.EmailExist("novo@admin.com", "999.999.999-99"), Times.Once);
            mockService.Verify(service => service.IsAdmAsync(It.IsAny<Pessoa>()), Times.Once);
            mockService.Verify(service => service.VerificaEdit(It.IsAny<Pessoa>()), Times.Once);
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.Is<Pessoa>(p =>
                p.Cpf == "999.999.999-99" &&
                p.Nome == "Novo Admin" &&
                p.NomeCracha == "Novo" &&
                p.Email == "novo@admin.com" &&
                p.Telefone1 == "7999999999"), 0, 1), Times.Once);
            // Caminho de sucesso redireciona sem recarregar a lista de admins
            mockService.Verify(service => service.GetAllAdmAsync(), Times.Never);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Post_InvalidEmail()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.ValidaEmail(It.IsAny<string>())).Returns(false);
            mockService.Setup(service => service.GetAllAdmAsync()).ReturnsAsync(new List<Pessoa> { GetTargetPessoa() });
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "040.268.930-57"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());
            localController.ModelState.Clear();

            var viewModel = new GestaoAdministradorModel
            {
                Id = 4,
                Nome = "Novo Admin",
                Cpf = "999.999.999-99",
                Email = "email-invalido",
                Telefone1 = "7999999999"
            };

            var result = await localController.DefinirAdministrador(viewModel);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
            Assert.AreEqual(1, localController.ModelState.ErrorCount);
            Assert.IsTrue(localController.ModelState.ContainsKey("Email"));
            Assert.AreEqual("Por favor, digite um e-mail em um formato válido.", localController.ModelState["Email"].Errors[0].ErrorMessage);

            // Additional assertions - verify view model is returned with admin list
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoAdministradorModel));
            GestaoAdministradorModel model = (GestaoAdministradorModel)viewResult.ViewData.Model;
            Assert.AreEqual(1, model.Administradores.Count);
            Assert.AreEqual((uint)1, model.Administradores[0].Id);

            // Verify service was not called to create admin
            mockService.Verify(service => service.ValidaEmail("email-invalido"), Times.Once);
            mockService.Verify(service => service.EmailExist(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            mockService.Verify(service => service.IsAdmAsync(It.IsAny<Pessoa>()), Times.Never);
            mockService.Verify(service => service.VerificaEdit(It.IsAny<Pessoa>()), Times.Never);
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), 0, 1), Times.Never);
            mockService.Verify(service => service.GetAllAdmAsync(), Times.Once);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Post_EmailExists()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.ValidaEmail(It.IsAny<string>())).Returns(true);
            mockService.Setup(service => service.EmailExist(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            mockService.Setup(service => service.GetAllAdmAsync()).ReturnsAsync(new List<Pessoa> { GetTargetPessoa() });
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "040.268.930-57"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());
            localController.ModelState.Clear();

            var viewModel = new GestaoAdministradorModel
            {
                Id = 4,
                Nome = "Novo Admin",
                Cpf = "999.999.999-99",
                Email = "existente@gmail.com",
                Telefone1 = "7999999999"
            };

            var result = await localController.DefinirAdministrador(viewModel);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
            Assert.AreEqual(1, localController.ModelState.ErrorCount);
            Assert.IsTrue(localController.ModelState.ContainsKey("Email"));
            Assert.AreEqual("O e-mail informado já está em uso.", localController.ModelState["Email"].Errors[0].ErrorMessage);

            // Additional assertions
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoAdministradorModel));
            GestaoAdministradorModel model = (GestaoAdministradorModel)viewResult.ViewData.Model;
            Assert.AreEqual(1, model.Administradores.Count);
            Assert.AreEqual((uint)1, model.Administradores[0].Id);

            // Verify service was not called to create admin
            mockService.Verify(service => service.ValidaEmail("existente@gmail.com"), Times.Once);
            mockService.Verify(service => service.EmailExist("existente@gmail.com", "999.999.999-99"), Times.Once);
            mockService.Verify(service => service.IsAdmAsync(It.IsAny<Pessoa>()), Times.Never);
            mockService.Verify(service => service.VerificaEdit(It.IsAny<Pessoa>()), Times.Never);
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), 0, 1), Times.Never);
            mockService.Verify(service => service.GetAllAdmAsync(), Times.Once);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Post_AlreadyAdmin()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.ValidaEmail(It.IsAny<string>())).Returns(true);
            mockService.Setup(service => service.EmailExist(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            mockService.Setup(service => service.IsAdmAsync(It.IsAny<Pessoa>())).ReturnsAsync(true);
            mockService.Setup(service => service.GetAllAdmAsync()).ReturnsAsync(new List<Pessoa> { GetTargetPessoa() });
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "040.268.930-57"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());
            localController.ModelState.Clear();

            var viewModel = new GestaoAdministradorModel
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                Cpf = "040.268.930-57",
                Email = "email@gmail.com",
                Telefone1 = "7999990011"
            };

            var result = await localController.DefinirAdministrador(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.IsNull(redirectResult.ControllerName);
            Assert.AreEqual("Já existe um administrador cadastrado com esse CPF.", localController.TempData["ErrorMessage"]);
            Assert.IsNull(localController.TempData["SuccessMessage"]);

            mockService.Verify(service => service.ValidaEmail("email@gmail.com"), Times.Once);
            mockService.Verify(service => service.EmailExist("email@gmail.com", "040.268.930-57"), Times.Once);
            mockService.Verify(service => service.IsAdmAsync(It.Is<Pessoa>(p => p.Cpf == "040.268.930-57")), Times.Once);
            mockService.Verify(service => service.VerificaEdit(It.IsAny<Pessoa>()), Times.Never);
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()), Times.Never);
            mockService.Verify(service => service.GetAllAdmAsync(), Times.Never);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Post_ModelStateInvalid()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.GetAllAdmAsync()).ReturnsAsync(new List<Pessoa> { GetTargetPessoa() });
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            localController.ModelState.AddModelError("Nome", "Campo requerido");

            var viewModel = new GestaoAdministradorModel
            {
                Id = 4,
                Nome = "",
                Cpf = "999.999.999-99",
                Email = "novo@admin.com",
                Telefone1 = "7999999999"
            };

            var result = await localController.DefinirAdministrador(viewModel);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
            Assert.IsFalse(localController.ModelState.IsValid);
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoAdministradorModel));
            GestaoAdministradorModel model = (GestaoAdministradorModel)viewResult.ViewData.Model;
            Assert.AreEqual(1, model.Administradores.Count);

            mockService.Verify(service => service.ValidaEmail(It.IsAny<string>()), Times.Never);
            mockService.Verify(service => service.EmailExist(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            mockService.Verify(service => service.IsAdmAsync(It.IsAny<Pessoa>()), Times.Never);
            mockService.Verify(service => service.VerificaEdit(It.IsAny<Pessoa>()), Times.Never);
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()), Times.Never);
            mockService.Verify(service => service.GetAllAdmAsync(), Times.Once);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Post_VerificaEditFails()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.ValidaEmail(It.IsAny<string>())).Returns(true);
            mockService.Setup(service => service.EmailExist(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            mockService.Setup(service => service.IsAdmAsync(It.IsAny<Pessoa>())).ReturnsAsync(false);
            mockService.Setup(service => service.VerificaEdit(It.IsAny<Pessoa>())).ReturnsAsync(false);
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());
            localController.ModelState.Clear();

            var viewModel = new GestaoAdministradorModel
            {
                Id = 4,
                Nome = "Novo Admin",
                Cpf = "999.999.999-99",
                Email = "novo@admin.com",
                Telefone1 = "7999999999"
            };

            var result = await localController.DefinirAdministrador(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.AreEqual("Erro ao cadastrar administrador.", localController.TempData["ErrorMessage"]);
            Assert.IsNull(localController.TempData["SuccessMessage"]);

            mockService.Verify(service => service.VerificaEdit(It.Is<Pessoa>(p => p.Cpf == "999.999.999-99" && p.NomeCracha == "Novo")), Times.Once);
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod()]
        public async Task DefinirAdministradorTest_Post_CreateIdentityFails()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.ValidaEmail(It.IsAny<string>())).Returns(true);
            mockService.Setup(service => service.EmailExist(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            mockService.Setup(service => service.IsAdmAsync(It.IsAny<Pessoa>())).ReturnsAsync(false);
            mockService.Setup(service => service.VerificaEdit(It.IsAny<Pessoa>())).ReturnsAsync(true);
            mockService.Setup(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), 0, 1)).ReturnsAsync(false);
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());
            localController.ModelState.Clear();

            var viewModel = new GestaoAdministradorModel
            {
                Id = 4,
                Nome = "Novo Admin",
                Cpf = "999.999.999-99",
                Email = "novo@admin.com",
                Telefone1 = "7999999999"
            };

            var result = await localController.DefinirAdministrador(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.AreEqual("Erro ao cadastrar administrador.", localController.TempData["ErrorMessage"]);
            Assert.IsNull(localController.TempData["SuccessMessage"]);

            mockService.Verify(service => service.VerificaEdit(It.IsAny<Pessoa>()), Times.Once);
            mockService.Verify(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), 0, 1), Times.Once);
        }

        [TestMethod()]
        public async Task EnviarEmailSenhaTest_Post_Valid()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            var pessoa = GetTargetPessoa();
            mockService.Setup(service => service.Get(pessoa.Id)).Returns(pessoa);
            mockService.Setup(service => service.EmailConfirmado(pessoa.Email)).Returns(true);
            mockService.Setup(service => service.GerarTokenAsync(pessoa.Cpf)).ReturnsAsync("test-token");
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, pessoa.Cpf),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.Url = new TestUrlHelper();
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());

            var viewModel = new PessoaModel { Id = pessoa.Id };

            var result = await localController.EnviarEmailSenha(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.IsNull(redirectResult.ControllerName);
            Assert.AreEqual("E-mail de redefinição enviado com sucesso!", localController.TempData["SuccessMessage"]);
            Assert.IsNull(localController.TempData["ErrorMessage"]);
            mockService.Verify(service => service.Get(pessoa.Id), Times.Once);
            mockService.Verify(service => service.EmailConfirmado(pessoa.Email), Times.Once);
            mockService.Verify(service => service.GerarTokenAsync(pessoa.Cpf), Times.Once);
            IEmailSenderMock.Verify(sender => sender.SendEmailAsync(
                pessoa.Email,
                "Redefinição de Senha",
                It.Is<string>(msg => msg.Contains(pessoa.Nome) && msg.Contains("test-token"))), Times.Once);
        }

        [TestMethod()]
        public async Task EnviarEmailSenhaTest_Post_UserNotFound()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            mockService.Setup(service => service.Get(999)).Returns((Pessoa?)null);
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "040.268.930-57"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());

            var viewModel = new PessoaModel { Id = 999 };

            var result = await localController.EnviarEmailSenha(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.AreEqual("Usuário não encontrado.", localController.TempData["ErrorMessage"]);
            Assert.IsNull(localController.TempData["SuccessMessage"]);
            mockService.Verify(service => service.Get(999), Times.Once);
            mockService.Verify(service => service.EmailConfirmado(It.IsAny<string>()), Times.Never);
            mockService.Verify(service => service.GerarTokenAsync(It.IsAny<string>()), Times.Never);
            IEmailSenderMock.Verify(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod()]
        public async Task EnviarEmailSenhaTest_Post_InvalidEmail()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            var pessoa = GetTargetPessoa();
            pessoa.Email = "email-invalido";
            mockService.Setup(service => service.Get(pessoa.Id)).Returns(pessoa);
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());

            var viewModel = new PessoaModel { Id = pessoa.Id };

            var result = await localController.EnviarEmailSenha(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.AreEqual("Não é possível enviar a mensagem, o email está incorreto.", localController.TempData["ErrorMessage"]);
            Assert.IsNull(localController.TempData["SuccessMessage"]);
            mockService.Verify(service => service.Get(pessoa.Id), Times.Once);
            mockService.Verify(service => service.EmailConfirmado(It.IsAny<string>()), Times.Never);
            mockService.Verify(service => service.GerarTokenAsync(It.IsAny<string>()), Times.Never);
            IEmailSenderMock.Verify(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod()]
        public async Task EnviarEmailSenhaTest_Post_EmailNotConfirmed()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosService = new Mock<IEstadosbrasilService>();
            var pessoa = GetTargetPessoa();
            mockService.Setup(service => service.Get(pessoa.Id)).Returns(pessoa);
            mockService.Setup(service => service.EmailConfirmado(pessoa.Email)).Returns(false);
            var IEmailSenderMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper, IEmailSenderMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, pessoa.Cpf),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, "mock"));

            localController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            localController.TempData = new TempDataDictionary(localController.HttpContext, Mock.Of<ITempDataProvider>());

            var viewModel = new PessoaModel { Id = pessoa.Id };

            var result = await localController.EnviarEmailSenha(viewModel);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectResult.ActionName);
            Assert.AreEqual("E-mail não foi confirmado.", localController.TempData["ErrorMessage"]);
            Assert.IsNull(localController.TempData["SuccessMessage"]);
            mockService.Verify(service => service.Get(pessoa.Id), Times.Once);
            mockService.Verify(service => service.EmailConfirmado(pessoa.Email), Times.Once);
            mockService.Verify(service => service.GerarTokenAsync(It.IsAny<string>()), Times.Never);
            IEmailSenderMock.Verify(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private PessoaModel GetNewPessoa()
        {
            return new PessoaModel
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                NomeCracha = "Sodré",
                Cpf = "040.268.930-57",
                Sexo = "M",
                Cep = "48370-000",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irece",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
            };
        }

        private static Pessoa GetTargetPessoa()
        {
            return new Pessoa
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                NomeCracha = "Sodré",
                Cpf = "040.268.930-57",
                Sexo = "M",
                Cep = "48370-000",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irece",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = "NULL",
            };
        }

        private PessoaModel GetTargetPessoaModel()
        {
            return new PessoaModel
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                NomeCracha = "Sodré",
                Cpf = "040.268.930-57",
                Sexo = "M",
                Cep = "48370-000",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irece",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = "NULL",
            };
        }

        private IEnumerable<Pessoa> GetTestPessoas()
        {
            return new List<Pessoa>
            {
                new Pessoa
                {
                    Id = 1,
                    Nome = "João Vitor Sodré",
                    NomeCracha = "Sodré",
                    Cpf = "040.268.930-57",
                    Sexo = "M",
                    Cep = "48370-000",
                    Rua = "Avenida Principal",
                    Bairro = "Centro",
                    Cidade = "Irece",
                    Estado = "BA",
                    Numero = "s/n",
                    Complemento = "casa",
                    Email = "email@gmail.com",
                    Telefone1 = "7999990011",
                    Telefone2 = "NULL",
                },
                new Pessoa
                {
                    Id = 2,
                    Nome = "Nagibe Santos Wanus Junior",
                    NomeCracha = "Nagibe Junior",
                    Cpf = "917.091.250-55",
                    Sexo = "M",
                    Cep = "45566-000",
                    Rua = "Rua Severino Vieira",
                    Bairro = "Centro",
                    Cidade = "Esplanada",
                    Estado = "BA",
                    Numero = "147",
                    Complemento = "casa",
                    Email = "nagibejr@gmail.com",
                    Telefone1 = "7599643467",
                    Telefone2 = "NULL",
                },
                new Pessoa
                {
                    Id = 3,
                    Nome = "Marcos Venicios da Palma Dias",
                    NomeCracha = "Marcos Venicios",
                    Cpf = "206.015.300-04",
                    Sexo = "M",
                    Cep = "45340-086",
                    Rua = "Rua da Linha",
                    Bairro = "Centro",
                    Cidade = "Esplanada",
                    Estado = "BA",
                    Numero = "s/n",
                    Complemento = "casa",
                    Email = "muzanpvp@gmail.com",
                    Telefone1 = "7999001133",
                    Telefone2 = "NULL",
                },
            };
        }
    }
}