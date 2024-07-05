using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class PessoaControllerTests
    {
        private static PessoaController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IPessoaService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new PessoaProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestPessoas());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetPessoa());
            mockService.Setup(service => service.Create(It.IsAny<Pessoa>()))
                .Verifiable();
            controller = new PessoaController(mockService.Object, mapper);
        }

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<PessoaModel>));

            List<PessoaModel>? lista = (List<PessoaModel>)viewResult.ViewData.Model;
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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(PessoaModel));
            PessoaModel pessoaModel = (PessoaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, pessoaModel.Id);
            Assert.AreEqual("João Vitor Sodré", pessoaModel.Nome);
            Assert.AreEqual("Sodré", pessoaModel.NomeCracha);
            Assert.AreEqual("12246232367", pessoaModel.Cpf);
            Assert.AreEqual("M", pessoaModel.Sexo);
            Assert.AreEqual("45340086", pessoaModel.Cep);
            Assert.AreEqual("Avenida Principal", pessoaModel.Rua);
            Assert.AreEqual("Centro", pessoaModel.Bairro);
            Assert.AreEqual("Irece", pessoaModel.Cidade);
            Assert.AreEqual("BA", pessoaModel.Estado);
            Assert.AreEqual("s/n", pessoaModel.Numero);
            Assert.AreEqual("casa", pessoaModel.Complemento);
            Assert.AreEqual("email@gmail.com", pessoaModel.Email);
            Assert.AreEqual("7999900113344", pessoaModel.Telefone1);
            Assert.AreEqual("NULL", pessoaModel.Telefone2);
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
            var result = controller.Create(GetNewPessoa());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Nome", "Campo requerido");
            controller.ModelState.AddModelError("NomeCracha", "Campo requerido");
            controller.ModelState.AddModelError("Cpf", "Campo requerido");
            controller.ModelState.AddModelError("Sexo", "Campo requerido");
            controller.ModelState.AddModelError("Email", "Campo requerido");
            // Act
            var result = controller.Create(GetNewPessoa());

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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(PessoaModel));
            PessoaModel pessoaModel = (PessoaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, pessoaModel.Id);
            Assert.AreEqual("João Vitor Sodré", pessoaModel.Nome);
            Assert.AreEqual("Sodré", pessoaModel.NomeCracha);
            Assert.AreEqual("12246232367", pessoaModel.Cpf);
            Assert.AreEqual("M", pessoaModel.Sexo);
            Assert.AreEqual("45340086", pessoaModel.Cep);
            Assert.AreEqual("Avenida Principal", pessoaModel.Rua);
            Assert.AreEqual("Centro", pessoaModel.Bairro);
            Assert.AreEqual("Irece", pessoaModel.Cidade);
            Assert.AreEqual("BA", pessoaModel.Estado);
            Assert.AreEqual("s/n", pessoaModel.Numero);
            Assert.AreEqual("casa", pessoaModel.Complemento);
            Assert.AreEqual("email@gmail.com", pessoaModel.Email);
            Assert.AreEqual("7999900113344", pessoaModel.Telefone1);
            Assert.AreEqual("NULL", pessoaModel.Telefone2);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.Edit(GetTargetPessoaModel().Id, GetTargetPessoaModel());

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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(PessoaModel));
            PessoaModel pessoaModel = (PessoaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, pessoaModel.Id);
            Assert.AreEqual("João Vitor Sodré", pessoaModel.Nome);
            Assert.AreEqual("Sodré", pessoaModel.NomeCracha);
            Assert.AreEqual("12246232367", pessoaModel.Cpf);
            Assert.AreEqual("M", pessoaModel.Sexo);
            Assert.AreEqual("45340086", pessoaModel.Cep);
            Assert.AreEqual("Avenida Principal", pessoaModel.Rua);
            Assert.AreEqual("Centro", pessoaModel.Bairro);
            Assert.AreEqual("Irece", pessoaModel.Cidade);
            Assert.AreEqual("BA", pessoaModel.Estado);
            Assert.AreEqual("s/n", pessoaModel.Numero);
            Assert.AreEqual("casa", pessoaModel.Complemento);
            Assert.AreEqual("email@gmail.com", pessoaModel.Email);
            Assert.AreEqual("7999900113344", pessoaModel.Telefone1);
            Assert.AreEqual("NULL", pessoaModel.Telefone2);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetPessoaModel().Id, GetTargetPessoaModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        private PessoaModel GetNewPessoa()
        {
            return new PessoaModel
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                NomeCracha = "Sodré",
                Cpf = "12246232367",
                Sexo = "M",
                Cep = "45340086",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irece",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999900113344",
                Telefone2 = "NULL",
            };
        }
        private static Pessoa GetTargetPessoa()
        {
            return new Pessoa
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                NomeCracha = "Sodré",
                Cpf = "12246232367",
                Sexo = "M",
                Cep = "45340086",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irece",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999900113344",
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
                Cpf = "12246232367",
                Sexo = "M",
                Cep = "45340086",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irece",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999900113344",
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
                        Cpf = "12246232367",
                        Sexo = "M",
                        Cep = "45340086",
                        Rua = "Avenida Principal",
                        Bairro = "Centro",
                        Cidade = "Irece",
                        Estado = "BA",
                        Numero = "s/n",
                        Complemento = "casa",
                        Email = "email@gmail.com",
                        Telefone1 = "7999900113344",
                        Telefone2 = "NULL",
                    },
                new Pessoa
                {
                        Id = 2,
                        Nome = "Nagibe Santos Wanus Junior",
                        NomeCracha = "Nagibe Junior",
                        Cpf = "12345345678",
                        Sexo = "M",
                        Cep = "45566000",
                        Rua = "Rua Severino Vieira",
                        Bairro = "Centro",
                        Cidade = "Esplanada",
                        Estado = "BA",
                        Numero = "147",
                        Complemento = "casa",
                        Email = "nagibejr@gmail.com",
                        Telefone1 = "75999643467",
                        Telefone2 = "NULL",
                    },
                new Pessoa
                {
                        Id = 3,
                        Nome = "Marcos Venicios da Palma Dias",
                        NomeCracha = "Marcos Venicios",
                        Cpf = "12247894667",
                        Sexo = "M",
                        Cep = "45340086",
                        Rua = "Rua da Linha",
                        Bairro = "Centro",
                        Cidade = "Esplanada",
                        Estado = "BA",
                        Numero = "s/n",
                        Complemento = "casa",
                        Email = "muzanpvp@gmail.com",
                        Telefone1 = "7999900113344",
                        Telefone2 = "NULL",
                    },
            };
        }
    }
}