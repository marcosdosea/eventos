using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
using System.Collections.Generic;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class ColaboradorControllerTests
    {
        private static ColaboradorController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IColaboradorService>();
            var mockEstadosbrasilService = new Mock<IEstadosbrasilService>();

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new PessoaProfile())).CreateMapper();

            mockService.Setup(service => service.GetColaboradoresAsync())
                .ReturnsAsync(GetTestColaboradores());
            mockService.Setup(service => service.CreateAsync(It.IsAny<Pessoa>()))
                .ReturnsAsync(true)
                .Verifiable();
            controller = new ColaboradorController(mockService.Object, mapper);
        }

        [TestMethod()]
        public async Task IndexTest()
        {
            // Act
            var result = await controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ColaboradorModel));

            ColaboradorModel? model = (ColaboradorModel)viewResult.ViewData.Model;
            Assert.AreEqual(3, model.Colaboradores.Count());
        }

        [TestMethod()]
        public async Task CreateTest_Get()
        {
            // Act
            var result = await controller.Create();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod()]
        public async Task CreateTest_Valid()
        {
            // Arrange
            controller.ModelState.Clear(); // Certifique-se de que o ModelState está limpo
            var mockService = new Mock<IColaboradorService>();
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();

            controller = new ColaboradorController(mockService.Object, mapper);

            // Act
            var result = await controller.Create(GetNewColaboradorModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);

            // Verifique se o método CreateAsync do serviço foi chamado
            mockService.Verify(service => service.CreateAsync(It.IsAny<Pessoa>()), Times.Once);
        }

        [TestMethod()]
        public async Task CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.Clear(); // Certifique-se de que o ModelState está limpo
            var mockService = new Mock<IColaboradorService>();
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();

            controller = new ColaboradorController(mockService.Object, mapper);

            controller.ModelState.AddModelError("Nome", "Campo requerido");
            controller.ModelState.AddModelError("Cpf", "Campo requerido");

            // Act
            var result = await controller.Create(GetNewColaboradorModel());

            // Assert
            Assert.AreEqual(2, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ColaboradorModel));
        }

        private ColaboradorModel GetNewColaboradorModel()
        {
            return new ColaboradorModel
            {
                Colaborador = new PessoaModel
                {
                    Nome = "Alexandre Moreira Silva",
                    NomeCracha = "Alexandre",
                    Cpf = "070.594.845-58",
                    Sexo = "M",
                    Cep = "48800-000",
                    Rua = "Avenida Principal",
                    Bairro = "Centro",
                    Cidade = "Porto da Folha",
                    Estado = "SE",
                    Numero = "1503",
                    Complemento = "casa",
                    Email = "email@gmail.com",
                    Telefone1 = "7999990011",
                }
            };
        }

        private IEnumerable<ColaboradorDTO> GetTestColaboradores()
        {
            return new List<ColaboradorDTO>
            {
                new ColaboradorDTO
                {
                    Id = 1,
               Nome = "Alexandre Moreira Silva",
                    NomeCracha = "Alexandre",
                    Cpf = "070.594.845-58",
                    Sexo = "M",
                    Cep = "48800-000",
                    Rua = "Avenida Principal",
                    Bairro = "Centro",
                    Cidade = "Porto da Folha",
                    Estado = "SE",
                    Numero = "1503",
                    Complemento = "casa",
                    Email = "email@gmail.com",
                    Telefone1 = "7999990011",
                    Telefone2 = null,
                    IsActive = true,
                    RegistrationDate = DateTime.Now,
                    LastLogin = null
                },
                new ColaboradorDTO
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
                    Telefone2 = null,
                    IsActive = true,
                    RegistrationDate = DateTime.Now,
                    LastLogin = null
                },
                new ColaboradorDTO
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
                    Telefone2 = null,
                    IsActive = true,
                    RegistrationDate = DateTime.Now,
                    LastLogin = null
                }
            };
        }
    }
}