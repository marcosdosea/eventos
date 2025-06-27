using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class ParticipanteControllerTests
    {
        private static ParticipanteController controller = null!;
        private static Mock<IParticipanteService> mockService = null!;
        private static IMapper mapper = null!;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            mockService = new Mock<IParticipanteService>();
            var mockEstadosbrasilService = new Mock<IEstadosbrasilService>();

            mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new PessoaProfile())).CreateMapper();

            mockService.Setup(service => service.GetParticipantesAsync())
                .ReturnsAsync(GetTestParticipantes());
            mockService.Setup(service => service.CreateAsync(It.IsAny<Pessoa>()))
                .Returns(Task.CompletedTask);
            mockService.Setup(service => service.DeleteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            controller = new ParticipanteController(mockService.Object, mapper);
        }

        [TestMethod()]
        public async Task IndexTest()
        {
            // Act
            var result = await controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ParticipanteModel));

            ParticipanteModel? lista = (ParticipanteModel)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Participantes?.Count());
        }

        [TestMethod()]
        public async Task CreateTest_Valid()
        {
            // Arrange
            controller.ModelState.Clear();
            var newController = new ParticipanteController(mockService.Object, mapper);

            // Act
            var result = await newController.Create(GetNewParticipanteModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);

            // Verifique se o método CreateAsync do serviço foi chamado
            mockService.Verify(service => service.CreateAsync(It.IsAny<Pessoa>()), Times.Once);
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
        public async Task CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.Clear();
            var newController = new ParticipanteController(mockService.Object, mapper);

            newController.ModelState.AddModelError("Nome", "Campo requerido");
            newController.ModelState.AddModelError("Cpf", "Campo requerido");

            // Act
            var result = await newController.Create(GetNewParticipanteModel());

            // Assert
            Assert.AreEqual(2, newController.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ParticipanteModel));
        }

        [TestMethod()]
        public async Task DetailsTest()
        {
            // Act
            var result = await controller.Details("040.268.930-57");

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ParticipanteModel));
            ParticipanteModel participanteModel = (ParticipanteModel)viewResult.ViewData.Model;
            Assert.AreEqual("João Vitor Sodré", participanteModel.Participante.Nome);
            Assert.AreEqual("Sodré", participanteModel.Participante.NomeCracha);
            Assert.AreEqual("040.268.930-57", participanteModel.Participante.Cpf);
            Assert.AreEqual("M", participanteModel.Participante.Sexo);
            Assert.AreEqual("48370-000", participanteModel.Participante.Cep);
            Assert.AreEqual("Avenida Principal", participanteModel.Participante.Rua);
            Assert.AreEqual("Centro", participanteModel.Participante.Bairro);
            Assert.AreEqual("Irecê", participanteModel.Participante.Cidade);
            Assert.AreEqual("BA", participanteModel.Participante.Estado);
            Assert.AreEqual("s/n", participanteModel.Participante.Numero);
            Assert.AreEqual("casa", participanteModel.Participante.Complemento);
            Assert.AreEqual("email@gmail.com", participanteModel.Participante.Email);
            Assert.AreEqual("7999990011", participanteModel.Participante.Telefone1);
            Assert.AreEqual(null, participanteModel.Participante.Telefone2);
        }

        [TestMethod()]
        public async Task EditTest_Get_Valid()
        {
            // Act
            var result = await controller.Edit("040.268.930-57");

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ParticipanteModel));
        }

        [TestMethod()]
        public async Task EditTest_Post_Valid()
        {
            // Arrange
            controller.ModelState.Clear();
            var participanteModel = GetNewParticipanteModel();

            // Act
            var result = await controller.Edit("040.268.930-57", participanteModel);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            mockService.Verify(service => service.CreateAsync(It.IsAny<Pessoa>()), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteTest_Post_Valid()
        {
            // Act
            var result = await controller.Delete("040.268.930-57");

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
            mockService.Verify(service => service.DeleteAsync("040.268.930-57"), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteTest_Get_Valid()
        {
            // Act
            var result = await controller.Delete("040.268.930-57");

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        private ParticipanteModel GetNewParticipanteModel()
        {
            return new ParticipanteModel
            {
                Participante = new PessoaModel
                {
                    Nome = "Haendel Hernan",
                    NomeCracha = "Haendel",
                    Cpf = "040.268.930-57",
                    Sexo = "M",
                    Cep = "49000-000",
                    Rua = "Avenida Ver. Olimpio Grande",
                    Bairro = "Porto",
                    Cidade = "Itabaiana",
                    Estado = "SE",
                    Numero = "100",
                    Complemento = "UFS",
                    Email = "email@gmail.com",
                    Telefone1 = "79000000000",
                }
            };
        }

        private IEnumerable<PessoaSimpleDTO> GetTestParticipantes()
        {
            return new List<PessoaSimpleDTO>
            {
                new PessoaSimpleDTO
                {
                    Cpf = "040.268.930-57",
                    Nome = "João Vitor Sodré",
                    Email = "email@gmail.com",
                    Telefone1 = "7999990011"
                },
                new PessoaSimpleDTO
                {
                    Cpf = "917.091.250-55",
                    Nome = "Nagibe Santos Wanus Junior",
                    Email = "nagibe@gmail.com",
                    Telefone1 = "7999990022"
                },
                new PessoaSimpleDTO
                {
                    Cpf = "123.456.789-00",
                    Nome = "Maria Silva",
                    Email = "maria@gmail.com",
                    Telefone1 = "7999990033"
                }
            };
        }

        private static Pessoa GetTargetParticipante()
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
                Cidade = "Irecê",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = null
            };
        }

        private static PessoaModel GetTargetParticipanteModel()
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
                Cidade = "Irecê",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = null
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
                    Cidade = "Irecê",
                    Estado = "BA",
                    Numero = "s/n",
                    Complemento = "casa",
                    Email = "email@gmail.com",
                    Telefone1 = "7999990011",
                    Telefone2 = null
                },
                new Pessoa
                {
                    Id = 2,
                    Nome = "Nagibe Santos Wanus Junior",
                    NomeCracha = "Nagibe Junior",
                    Cpf = "917.091.250-55",
                    Sexo = "M",
                    Cep = "45566-000",
                    Rua = "Rua das Flores",
                    Bairro = "Jardim",
                    Cidade = "Salvador",
                    Estado = "BA",
                    Numero = "123",
                    Complemento = "apto 101",
                    Email = "nagibe@gmail.com",
                    Telefone1 = "7999990022",
                    Telefone2 = null
                },
                new Pessoa
                {
                    Id = 3,
                    Nome = "Maria Silva",
                    NomeCracha = "Maria",
                    Cpf = "123.456.789-00",
                    Sexo = "F",
                    Cep = "49000-000",
                    Rua = "Rua das Palmeiras",
                    Bairro = "Centro",
                    Cidade = "Aracaju",
                    Estado = "SE",
                    Numero = "456",
                    Complemento = "casa",
                    Email = "maria@gmail.com",
                    Telefone1 = "7999990033",
                    Telefone2 = null
                }
            };
        }
    }
}