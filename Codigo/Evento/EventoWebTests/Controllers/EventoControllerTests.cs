using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using Core.DTO;
namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class EventoControllerTests
    {
        private static EventoController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IEventoService>();
            var mockServiceEstado = new Mock<IEstadosbrasilService>();
            var mockServiceInscricao = new Mock<IInscricaoService>();
            var mockServiceTipoevento = new Mock<ITipoeventoService>();
            var mockServiceAreaInteresse = new Mock<IAreaInteresseService>();
            var mockServicePessoa = new Mock<IPessoaService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new EventoProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestEventos());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetEvento());
            mockService.Setup(service => service.Create(It.IsAny<Evento>()))
                .Verifiable();

            mockServiceInscricao.Setup(service => service.GetByEventoAndPapel(1,1))
    .Returns(GetTestInscricoes());

            controller = new EventoController(mockService.Object, mapper, mockServiceEstado.Object, mockServiceInscricao.Object, mockServiceTipoevento.Object, mockServiceAreaInteresse.Object,mockServicePessoa.Object);
        }

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<EventoModel>));

            List<EventoModel>? lista = (List<EventoModel>)viewResult.ViewData.Model;
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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
            EventoModel eventoModel = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Cep);
            Assert.AreEqual("SE", eventoModel.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Cidade);
            Assert.AreEqual("Porto", eventoModel.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Rua);
            Assert.AreEqual("s/n", eventoModel.Numero);
            Assert.AreEqual("Universidade", eventoModel.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, eventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.CargaHoraria);
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
            var result = controller.Create(GetNewEvento());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Nome", "Nome do Evento é obrigatório");

            // Act
            var result = controller.Create(GetNewEvento());

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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventocreateModel));
            EventocreateModel eventoModel = (EventocreateModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Evento.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Evento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.Evento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.Evento.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.Evento.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Evento.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.Evento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.Evento.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.Evento.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Evento.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.Evento.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.Evento.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Evento.Cep);
            Assert.AreEqual("SE", eventoModel.Evento.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Evento.Cidade);
            Assert.AreEqual("Porto", eventoModel.Evento.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Evento.Rua);
            Assert.AreEqual("s/n", eventoModel.Evento.Numero);
            Assert.AreEqual("Universidade", eventoModel.Evento.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.Evento.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.Evento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, eventoModel.Evento.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.Evento.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.Evento.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.Evento.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.Evento.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.Evento.CargaHoraria);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.Edit(GetTargetEventoModelEdit().Evento.Id, GetTargetEventoModelEdit());

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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
            EventoModel eventoModel = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Cep);
            Assert.AreEqual("SE", eventoModel.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Cidade);
            Assert.AreEqual("Porto", eventoModel.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Rua);
            Assert.AreEqual("s/n", eventoModel.Numero);
            Assert.AreEqual("Universidade", eventoModel.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, eventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetEventoModel().Id, GetTargetEventoModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void GestaoPapel_Get_Valid()
        {
            // Act
            var result = controller.CreatePessoaPapel(GetTargetEventoModel().Id, GetTargetPapel().Id);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoPapelModel));
        }

        [TestMethod()]
        public void GestaoPapel_Post_Valid()
        {
            // Act
            var result = controller.CreatePessoaPapel(GetNewGestaoPapel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);

        }

        [TestMethod()]
        public void DeletePessoaPapel_Post_Valid()
        {
            // Act
            var result = controller.DeletePessoaPapel(1, 1, 1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
        }

        private EventocreateModel GetNewEvento()
        {
            var eventoModel = new EventoModel
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
            return new EventocreateModel
            {
                Evento = eventoModel,
            };
        }

        private GestaoPapelModel GetNewGestaoPapel()
        {
            var pessoa = new Pessoa
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

            var evento = new EventoSimpleDTO
            {
                Id = 1,
                Nome = "SEMINFO",
            };

            var papel = new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };

            return new GestaoPapelModel
            {
                Pessoa = pessoa,
                Evento = evento,
                IdPapel = papel.Id,
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

        private static EventoSimpleDTO GetTargetEventoSimple()
        {
            return new EventoSimpleDTO
            {
                Id = 1,
                Nome = "SEMINFO",
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

        private static Papel GetTargetPapel()
        {
            return new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };
        }

        private EventocreateModel GetTargetEventoModelEdit()
        {
            var eventoModel = new EventoModel
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
            return new EventocreateModel
            {
                Evento = eventoModel,
            };
        }

        private EventoModel GetTargetEventoModel()
        {
            return new EventoModel
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


        private IEnumerable<Inscricaopessoaevento> GetTestInscricoes()
        {
            var pessoa1 = new Pessoa    
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

            var pessoa2 = new Pessoa
            {
                Id = 2,
                Nome = "Nagibe Santos Wanus Junior",
                NomeCracha = "Nagibe Junior",
                Cpf = "12343455678",
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
            };

            var pessoa3 = new Pessoa
            {
                Id = 3,
                Nome = "Marcos Venicios da Palma Dias",
                NomeCracha = "Marcos Venicios",
                Cpf = "12244678667",
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
            };

            var evento = new EventoSimpleDTO
            {
                Id = 1,
                Nome = "SEMINFO",
            };

            var papel = new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };

            return new List<Inscricaopessoaevento>
            {
                new Inscricaopessoaevento
                {
                    IdPessoa = pessoa1.Id,
                    IdEvento = evento.Id,
                    IdPapel = papel.Id,
                },

                new Inscricaopessoaevento
                {
                    IdPessoa = pessoa2.Id,
                    IdEvento = evento.Id,
                    IdPapel = papel.Id,
                },
                
                new Inscricaopessoaevento
                {
                    IdPessoa = pessoa3.Id,
                    IdEvento = evento.Id,
                    IdPapel = papel.Id,
                }


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
    }
}