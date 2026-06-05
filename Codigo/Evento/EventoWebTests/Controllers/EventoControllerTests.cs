using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTO;
using Microsoft.AspNetCore.Identity;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class EventoControllerTests
    {
        private static EventoController controller = null!;

        [TestInitialize]
        public void Initialize()
        {
            var mockService = new Mock<IEventoService>();
            var mockServiceEstado = new Mock<IEstadosbrasilService>();
            var mockServiceInscricao = new Mock<IInscricaoService>();
            var mockServiceTipoevento = new Mock<ITipoeventoService>();
            var mockServiceAreaInteresse = new Mock<IAreaInteresseService>();
            var mockServicePessoa = new Mock<IPessoaService>();
            var mockServiceSubevento = new Mock<ISubeventoService>();
            var mockUserStore = new Mock<IUserStore<UsuarioIdentity>>();
            var mockUserManager = new Mock<UserManager<UsuarioIdentity>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new EventoProfile());
            });
            var mapper = config.CreateMapper();

            mockService.Setup(s => s.GetAll()).Returns(GetTestEventos());
            mockServiceTipoevento.Setup(s => s.GetNomeById(1)).Returns("SEMINARIO");
            mockServiceTipoevento.Setup(s => s.GetNomeById(2)).Returns("WORKSHOP");

            mockService.Setup(s => s.Get(1)).Returns(GetTestEventos().First());
            mockService.Setup(s => s.Create(It.IsAny<Evento>())).Returns(1U);
            mockService.Setup(s => s.Edit(It.IsAny<Evento>(), It.IsAny<List<uint>>()));
            mockService.Setup(s => s.Delete(1));

            controller = new EventoController(
                mockUserManager.Object,
                mockService.Object,
                mapper,
                mockServiceEstado.Object,
                mockServiceInscricao.Object,
                mockServiceTipoevento.Object,
                mockServiceAreaInteresse.Object,
                mockServicePessoa.Object,
                mockServiceSubevento.Object
            );
        }

        [TestMethod()]
        public void IndexTest_Valid()
        {
            var result = controller.Index();

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<EventoModel>));

            var list = (List<EventoModel>)viewResult.ViewData.Model;
            Assert.AreEqual(2, list.Count);
        }

        [TestMethod()]
        public void DetailsTest_Valid()
        {
            var result = controller.Details(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));

            var model = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINF", model.Nome);
        }

        [TestMethod()]
        public void CreateTest_Get_Valid()
        {
            var result = controller.Create();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod()]
        public void CreateTest_Post_Valid()
        {
            var model = new EventoModel
            {
                Id = 1,
                Nome = "SEMINF",
                Descricao = "Evento para seminário de informática",
                DataInicio = new DateTime(2024, 12, 1, 7, 30, 0),
                DataFim = new DateTime(2024, 12, 5, 12, 30, 0),
                InscricaoGratuita = 1,
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 11, 1, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 11, 5, 12, 30, 0),
                VagasOfertadas = 100,
                VagasReservadas = 20,
                VagasDisponiveis = 80,
                CargaHoraria = 40,
                Local = "Auditório Central",
                IdTipoEvento = 1,
            };

            var result = controller.Create(model);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectResult.ActionName);
        }

        [TestMethod()]
        public void EditTest_Get_Valid()
        {
            var result = controller.Edit(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));

            var model = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINF", model.Nome);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            var model = new EventoModel
            {
                Id = 1,
                Nome = "SEMINF ALTERADO",
                Descricao = "Evento para seminário de informática",
                DataInicio = new DateTime(2024, 12, 1, 7, 30, 0),
                DataFim = new DateTime(2024, 12, 5, 12, 30, 0),
                InscricaoGratuita = 1,
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 11, 1, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 11, 5, 12, 30, 0),
                VagasOfertadas = 100,
                VagasReservadas = 20,
                VagasDisponiveis = 80,
                CargaHoraria = 40,
                Local = "Auditório Central",
                IdTipoEvento = 1,
            };

            var result = controller.Edit(1, model);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectResult.ActionName);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            var result = controller.Delete(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));

            var model = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINF", model.Nome);
        }

        [TestMethod()]
        public void DeleteTest_Post_Valid()
        {
            var model = new EventoModel { Id = 1 };
            var result = controller.Delete(1, model);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectResult.ActionName);
        }

        private static List<Evento> GetTestEventos()
        {
            return new List<Evento>
            {
                new Evento
                {
                    Id = 1,
                    Nome = "SEMINF",
                    Descricao = "Evento para seminário de informática",
                    DataInicio = new DateTime(2024, 12, 1, 7, 30, 0),
                    DataFim = new DateTime(2024, 12, 5, 12, 30, 0),
                    InscricaoGratuita = 1,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 11, 1, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 11, 5, 12, 30, 0),
                    EmailEvento = "seminf@gmail.com",
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
                    IdTipoEvento = 2,
                    VagasOfertadas = 500,
                    VagasReservadas = 100,
                    VagasDisponiveis = 400,
                    CargaHoraria = 20,
                }
            };
        }
    }
}