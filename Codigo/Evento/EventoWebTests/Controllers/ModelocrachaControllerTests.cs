using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoMapper;
using Core.Service;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using EventoWeb.Mappers;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using System;

namespace EventoWeb.Controllers.Tests
{
    [TestClass]
    public class ModelocrachaControllerTests
    {
        private static ModelocrachaController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IModelocrachaService>();

            IMapper mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ModeloCrachaProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestModelocracha());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetModelocracha());
            mockService.Setup(service => service.Create(It.IsAny<Modelocracha>()))
                .Verifiable();
            controller = new ModelocrachaController(mockService.Object, mapper);
        }

        [TestMethod]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<ModelocrachaModel>));

            List<ModelocrachaModel>? lista = (List<ModelocrachaModel>)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Count);
        }

        [TestMethod]
        public void DetailsTest()
        {
            // Act
            var result = controller.Details(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
            ModelocrachaModel modelocrachaModel = (ModelocrachaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, modelocrachaModel.Id);
            Assert.AreEqual((uint)1, modelocrachaModel.IdEvento);
            Assert.AreEqual("Texto 1", modelocrachaModel.Texto);
            Assert.AreEqual(1, modelocrachaModel.Qrcode);
        }

        [TestMethod]
        public void CreateTest()
        {
            // Act
            var result = controller.Create();

            // Assert 
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void CreateTest_Valid()
        {
            // Act
            var result = controller.Create(GetNewModelocracha());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod]
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Texto", "Texto do Modelo de Cracha é obrigatório");

            // Act
            var result = controller.Create(GetNewModelocracha());

            // Assert
            Assert.AreEqual(1, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod]
        public void EditTest_Get_Valid()
        {
            // Act
            var result = controller.Edit(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
            ModelocrachaModel modelocrachaModel = (ModelocrachaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, modelocrachaModel.Id);
            Assert.AreEqual((uint)1, modelocrachaModel.IdEvento);
            Assert.AreEqual("Texto 1", modelocrachaModel.Texto);
            Assert.AreEqual(1, modelocrachaModel.Qrcode);
        }

        [TestMethod]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.Edit(GetTargetModelocrachaModel().Id, GetTargetModelocrachaModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod]
        public void DeleteTest_Post_Valid()
        {
            // Act
            var result = controller.Delete(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(ModelocrachaModel));
            ModelocrachaModel modelocrachaModel = (ModelocrachaModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, modelocrachaModel.Id);
            Assert.AreEqual((uint)1, modelocrachaModel.IdEvento);
        }

        [TestMethod]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetModelocrachaModel().Id, GetTargetModelocrachaModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        private ModelocrachaModel GetNewModelocracha()
        {
            var formFileMock = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "test.png";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            formFileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            formFileMock.Setup(_ => _.FileName).Returns(fileName);
            formFileMock.Setup(_ => _.Length).Returns(ms.Length);

            return new ModelocrachaModel
            {
                Id = 7,
                IdEvento = 4,
                Logotipo = formFileMock.Object,
                Texto = "Texto 4",
                Qrcode = 1
            };
        }


        private static Modelocracha GetTargetModelocracha()
        {
            return new Modelocracha
            {
                Id = 1,
                IdEvento = 1,
                Logotipo = new byte[] { 0x20, 0x20 },
                Texto = "Texto 1",
                Qrcode = 1
            };
        }

        private ModelocrachaModel GetTargetModelocrachaModel()
        {
            var formFileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(new byte[] { 0x20, 0x20 });
            formFileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            formFileMock.Setup(_ => _.Length).Returns(ms.Length);

            return new ModelocrachaModel
            {
                Id = 1,
                IdEvento = 1,
                Logotipo = formFileMock.Object,
                Texto = "Texto 1",
                Qrcode = 1
            };
        }

        private IEnumerable<Modelocracha> GetTestModelocracha()
        {
            return new List<Modelocracha>
            {
                new Modelocracha
                {
                    Id = 1,
                    IdEvento = 1,
                    Logotipo = new byte[] { 0x20, 0x20 },
                    Texto = "Texto 1",
                    Qrcode = 1
                },
                new Modelocracha
                {
                    Id = 3,
                    IdEvento = 1,
                    Logotipo = new byte[] { 0x30, 0x30 },
                    Texto = "Texto 2",
                    Qrcode = 1
                },
                new Modelocracha
                {
                    Id = 5,
                    IdEvento = 1,
                    Logotipo = new byte[] { 0x50, 0x50 },
                    Texto = "Texto 3",
                    Qrcode = 1
                },
            };
        }
    }
}