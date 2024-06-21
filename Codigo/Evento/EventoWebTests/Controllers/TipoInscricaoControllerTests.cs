using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Service;
namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class TipoInscricaoControllerTests
    {
        private static TipoInscricaoController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<ITipoInscricaoService>();
            var mockServiceEvento = new Mock<IEventoService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new TipoInscricaoProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestTipoInscricoes());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetTipoInscricao());
            mockService.Setup(service => service.Create(It.IsAny<Tipoinscricao>()))
                .Verifiable();
            controller = new TipoInscricaoController(mockService.Object, mapper, mockServiceEvento.Object);
        }
        

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<TipoInscricaoModel>));

            List<TipoInscricaoModel>? lista = (List<TipoInscricaoModel>)viewResult.ViewData.Model;
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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(TipoInscricaoModel));
            TipoInscricaoModel tipoinscricaoModel = (TipoInscricaoModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, tipoinscricaoModel.IdEvento);
            Assert.AreEqual("Gratuita", tipoinscricaoModel.Descricao);
            Assert.AreEqual((decimal)1, tipoinscricaoModel.Valor);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), tipoinscricaoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), tipoinscricaoModel.Datafim);
            Assert.AreEqual((sbyte)1, tipoinscricaoModel.UsadaEvento);
            Assert.AreEqual((sbyte)1, tipoinscricaoModel.UsadaEvento);
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
            var result = controller.Create(GetNewTipoInscricao());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Descricao", "Descricao do Tipo da Incrição é obrigatória");

            // Act
            var result = controller.Create(GetNewTipoInscricao());

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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(TipoInscricaocreateModel));
            TipoInscricaocreateModel tipoinscricaoModel = (TipoInscricaocreateModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, tipoinscricaoModel.TipoInscricao.IdEvento);
            Assert.AreEqual("Gratuita", tipoinscricaoModel.TipoInscricao.Descricao);
            Assert.AreEqual((decimal)1, tipoinscricaoModel.TipoInscricao.Valor);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), tipoinscricaoModel.TipoInscricao.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), tipoinscricaoModel.TipoInscricao.Datafim);
            Assert.AreEqual((sbyte)1, tipoinscricaoModel.TipoInscricao.UsadaEvento);
            Assert.AreEqual((sbyte)1, tipoinscricaoModel.TipoInscricao.UsadaEvento);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.Edit(GetTargetTipoInscricaoModelEdit().TipoInscricao.Id, GetTargetTipoInscricaoModelEdit());

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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(TipoInscricaoModel));
            TipoInscricaoModel tipoinscricaoModel = (TipoInscricaoModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, tipoinscricaoModel.IdEvento);
            Assert.AreEqual("Gratuita", tipoinscricaoModel.Descricao);
            Assert.AreEqual((decimal)1, tipoinscricaoModel.Valor);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), tipoinscricaoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), tipoinscricaoModel.Datafim);
            Assert.AreEqual((sbyte)1, tipoinscricaoModel.UsadaEvento);
            Assert.AreEqual((sbyte)1, tipoinscricaoModel.UsadaEvento);
        }


        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetTipoInscricaoModel().Id, GetTargetTipoInscricaoModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        private TipoInscricaocreateModel GetNewTipoInscricao()
        {

            var tipoInscricaoModel = new TipoInscricaoModel
            {
                Id = 1,
                IdEvento = 1,
                Descricao = "Gratuita",
                Valor = 1,
                DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                UsadaEvento = 1,
                UsadaSubevento = 1,
            };
            return new TipoInscricaocreateModel
            {
                TipoInscricao = tipoInscricaoModel,
            };
        }

        private TipoInscricaocreateModel GetTargetTipoInscricaoModelEdit()
        {

            var tipoInscricaoModel = new TipoInscricaoModel
            {
                Id = 1,
                IdEvento = 1,
                Descricao = "Gratuita",
                Valor = 1,
                DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                UsadaEvento = 1,
                UsadaSubevento = 1,
            };
            return new TipoInscricaocreateModel
            {
                TipoInscricao = tipoInscricaoModel,
            };
        }
        private static Tipoinscricao GetTargetTipoInscricao()
        {
            return new Tipoinscricao
            {
                Id = 1,
                IdEvento = 1,
                Descricao = "Gratuita",
                Valor = 1,
                DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                UsadaEvento = 1,
                UsadaSubevento = 1,
            };
        }

        private TipoInscricaoModel GetTargetTipoInscricaoModel()
        {
            return new TipoInscricaoModel
            {
                Id = 1,
                IdEvento = 1,
                Descricao = "Gratuita",
                Valor = 1,
                DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                UsadaEvento = 1,
                UsadaSubevento = 1,
            };
        }

        private IEnumerable<Tipoinscricao> GetTestTipoInscricoes()
        {
            return new List<Tipoinscricao>
            {
                new Tipoinscricao
                {
                        Id = 1,
                        IdEvento = 1,
                        Descricao = "Gratuita",
                        Valor = 1,
                        DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                        Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                        UsadaEvento = 1,
                        UsadaSubevento = 1,
                    },
                new Tipoinscricao
                {
                        Id = 3,
                        IdEvento = 5,
                        Descricao = "Paga",
                        Valor = 1,
                        DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                        Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                        UsadaEvento = 1,
                        UsadaSubevento = 1,
                    },
                new Tipoinscricao
                {
                        Id = 5,
                        IdEvento = 3,
                        Descricao = "Meia Entrada",
                        Valor = 1,
                        DataInicio = new DateTime(2024, 09, 2, 7, 30, 0),
                        Datafim = new DateTime(2024, 09, 7, 12, 30, 0),
                        UsadaEvento = 1,
                        UsadaSubevento = 1,
                    },
            };
        }
    }
}