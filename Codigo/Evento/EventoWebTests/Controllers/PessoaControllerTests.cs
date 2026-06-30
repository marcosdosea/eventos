using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class PessoaControllerTests
    {
        private static PessoaController? controller;

        [TestInitialize]
        public void Initialize()
        {
            var mockService = new Mock<IPessoaService>();
            var mockEstadosbrasilService = new Mock<IEstadosbrasilService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new PessoaProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestPessoas());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetPessoa());
            mockService.Setup(service => service.CreatePessoaIdentityComPapelAsync(It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            mockService.Setup(service => service.Delete(It.IsAny<uint>()))
                .Returns(true);
            mockService.Setup(service => service.CreatePessoaIdentityComPapelAsync(GetTargetPessoa(),1,1));
            mockService.Setup(service => service.Edit(It.IsAny<Pessoa>()))
                .Returns(Task.CompletedTask); 
            controller = new PessoaController(mockService.Object, mockEstadosbrasilService.Object, mapper);
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

            controller = new PessoaController(mockService.Object, new Mock<IEstadosbrasilService>().Object,
            new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper());

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

            controller = new PessoaController(mockService.Object, new Mock<IEstadosbrasilService>().Object,
            new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper());

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

            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, mockEstadosService.Object, mapper);

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

            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();
            var localController = new PessoaController(mockService.Object, new Mock<IEstadosbrasilService>().Object, mapper);

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
        public void DeleteTest_Post_Valid()
        {
                    
            controller!.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            var result = controller!.DeleteConfirmed(GetTargetPessoaModel());
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectToActionResult.ActionName);


        }


        [TestMethod()]
        public void DeleteAdmTest_Post_Valid()
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

            var result = controller!.DeleteConfirmed(GetTargetPessoaModel());
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("DefinirAdministrador", redirectToActionResult.ActionName);


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