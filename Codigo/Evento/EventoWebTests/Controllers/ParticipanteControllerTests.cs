using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class ParticipanteControllerTests
    {
        private static ParticipanteController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IParticipanteService>();
            var mockEstadosbrasilService = new Mock<IEstadosbrasilService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new PessoaProfile())).CreateMapper();

            mockService.Setup(service => service.GetParticipantesAsync())
                .ReturnsAsync(GetTestParticipantes());
            mockService.Setup(service => service.CreateAsync(It.IsAny<Pessoa>()))
                .ReturnsAsync(true);
            //mockService.Setup(service => service.Create(It.IsAny<Pessoa>()))
                .Verifiable();
            controller = new ParticipanteController(mockService.Object, mapper);
        }

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ParticipanteModel));

            ParticipanteModel? lista = (ParticipanteModel)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Participantes.Count());
        }

        [TestMethod()]
        public async Task CreateTest_Valid()
        {
            // Arrange
            controller.ModelState.Clear(); // Certifique-se de que o ModelState está limpo
            var mockService = new Mock<IParticipanteService>();
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();

            controller = new ParticipanteController(mockService.Object, mapper);

            // Act
            var result = await controller.Create(GetNewParticipanteModel());

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
            controller.ModelState.Clear(); // Certifique-se de que o ModelState está limpo
            var mockService = new Mock<IParticipanteService>();
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper();

            controller = new ParticipanteController(mockService.Object, mapper);

            controller.ModelState.AddModelError("Nome", "Campo requerido");
            controller.ModelState.AddModelError("Cpf", "Campo requerido");

            // Act
            var result = await controller.Create(GetNewParticipanteModel());

            // Assert
            Assert.AreEqual(2, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ParticipanteModel));
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
            // Act
            var result = controller.Create();

            // Assert 
            Assert.IsInstanceOfType(result, typeof(ViewResult));
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
        private IEnumerable<ParticipanteDTO> GetTestParticipantes()
        {
            return new List<ParticipanteDTO>
            {
                new ParticipanteDTO
                {
                    Id = 1,
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
                    Telefone2 = null,
                    IsActive = true,
                    RegistrationDate = DateTime.Now,
                    LastLogin = null
                },
                new ParticipanteDTO
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
                new ParticipanteDTO
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
        public void EditTest_Post_Valid()
        {
            // Arrange
            controller.ModelState.Clear(); // Certifique-se de que o ModelState está limpo
            var mockService = new Mock<IParticipanteService>();

            controller = new ParticipanteController(mockService.Object, new MapperConfiguration(cfg => cfg.AddProfile(new PessoaProfile())).CreateMapper());

            // Act
            var result = controller.Edit(GetTargetParticipanteModel().Id, GetTargetParticipanteModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);


            // Verifique se o método Create do serviço foi chamado
            mockService.Verify(service => service.Edit(It.IsAny<Pessoa>()), Times.Once);
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
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetParticipanteModel().Id);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        /*private ParticipanteModel GetNewParticipante()
        {
            return new ParticipanteModel
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
        }*/

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
                Cidade = "Irece",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = "NULL",
            };
        }

        private static Pessoa GetTargetParticipanteModel()
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