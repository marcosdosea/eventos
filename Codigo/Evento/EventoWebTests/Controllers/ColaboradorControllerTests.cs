using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
using System.Collections.Generic;
using Core.DTO;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class ColaboradorControllerTests
    {
        private ColaboradorController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IColaboradorService>();
            var mockEstadosbrasilService = new Mock<IEstadosbrasilService>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PessoaProfile());
                cfg.CreateMap<PessoaSimpleDTO, ColaboradorDTO>();
            });
            IMapper mapper = config.CreateMapper();

            mockService.Setup(service => service.GetColaboradoresAsync())
                .Returns(Task.FromResult(GetTestPessoaSimpleDTOs()));
            mockService.Setup(service => service.CreateAsync(It.IsAny<Pessoa>()))
                .Returns(Task.FromResult(true))
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
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PessoaProfile());
                cfg.CreateMap<PessoaSimpleDTO, ColaboradorDTO>();
            });
            IMapper mapper = config.CreateMapper();

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
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PessoaProfile());
                cfg.CreateMap<PessoaSimpleDTO, ColaboradorDTO>();
            });
            IMapper mapper = config.CreateMapper();

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

        private IEnumerable<PessoaSimpleDTO> GetTestPessoaSimpleDTOs()
        {
            return new List<PessoaSimpleDTO>
            {
                new PessoaSimpleDTO { Cpf = "111.111.111-11", Nome = "Colaborador 1", Email = "colab1@email.com", Telefone1 = "999999999" },
                new PessoaSimpleDTO { Cpf = "222.222.222-22", Nome = "Colaborador 2", Email = "colab2@email.com", Telefone1 = "988888888" },
                new PessoaSimpleDTO { Cpf = "333.333.333-33", Nome = "Colaborador 3", Email = "colab3@email.com", Telefone1 = "977777777" }
            };
        }
    }
}