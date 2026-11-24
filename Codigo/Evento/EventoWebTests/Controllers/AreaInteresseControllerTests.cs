using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class AreaInteresseControllerTests
    {
        private static AreaInteresseController controller;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IAreaInteresseService>();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestAreaInteresses());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetAreaInteresse());
            mockService.Setup(service => service.Create(It.IsAny<Areainteresse>()))
                .Verifiable();
            controller = new AreaInteresseController(mockService.Object);
        }

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(IEnumerable<AreaInteresseModel>));

            IEnumerable<AreaInteresseModel>? lista = (IEnumerable<AreaInteresseModel>)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Count());
        }

        [TestMethod()]
        public void DetailsTest()
        {
            // Act
            var result = controller.Details(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(AreaInteresseModel));
            AreaInteresseModel areaInteresseModel = (AreaInteresseModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, areaInteresseModel.Id);
            Assert.AreEqual("Curso", areaInteresseModel.Nome);
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
            var result = controller.Create(GetNewAreaInteresse());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void CreateTest_Invalid()
        {
            // Arrange
            controller.ModelState.AddModelError("Nome", "Campo requerido");

            // Act
            var result = controller.Create(GetNewAreaInteresse());

			// Assert
			Assert.AreEqual(1, controller.ModelState.ErrorCount);
			Assert.IsInstanceOfType(result, typeof(ViewResult));
			ViewResult viewResult = (ViewResult)result;
			Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(AreaInteresseModel));
		}


        [TestMethod()]
        public void EditTest_Get_Valid()
        {
            // Act
            var result = controller.Edit(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(AreaInteresseModel));
            AreaInteresseModel areaInteresseModel = (AreaInteresseModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, areaInteresseModel.Id);
            Assert.AreEqual("Curso", areaInteresseModel.Nome);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            // Act
            var result = controller.Edit(GetTargetAreaInteresseModel().Id, GetTargetAreaInteresseModel());

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
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(AreaInteresseModel));
            AreaInteresseModel areaInteresseModel = (AreaInteresseModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, areaInteresseModel.Id);
            Assert.AreEqual("Curso", areaInteresseModel.Nome);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            // Act
            var result = controller.Delete(GetTargetAreaInteresseModel().Id, GetTargetAreaInteresseModel());

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        private AreaInteresseModel GetNewAreaInteresse()
        {
            return new AreaInteresseModel
            {
                Id = 4,
                Nome = "Workshop",
            };
        }

        [TestMethod()]
        public void CreateTest_NomeDuplicadoCaseInsensitive_RetornaViewComErroEmNome()
        {
            var result = controller.Create(new AreaInteresseModel { Nome = "  cUrSo  " });

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsTrue(controller.ModelState.ContainsKey("Nome"));
        }

        [TestMethod()]
        public void EditTest_NomeDuplicadoDeOutraArea_RetornaViewComErroEmNome()
        {
            var result = controller.Edit(1, new AreaInteresseModel { Id = 1, Nome = "PALESTRA" });

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsTrue(controller.ModelState.ContainsKey("Nome"));
        }

        [TestMethod()]
        public void AreaInteresseModel_MensagensValidacao_ReferemAreaDeInteresse()
        {
            var propId = typeof(AreaInteresseModel).GetProperty(nameof(AreaInteresseModel.Id))!;
            var reqId = (System.ComponentModel.DataAnnotations.RequiredAttribute)Attribute.GetCustomAttribute(propId, typeof(System.ComponentModel.DataAnnotations.RequiredAttribute))!;
            Assert.AreEqual("Código da Área de Interesse é obrigatório", reqId.ErrorMessage);

            var propNome = typeof(AreaInteresseModel).GetProperty(nameof(AreaInteresseModel.Nome))!;
            var reqNome = (System.ComponentModel.DataAnnotations.RequiredAttribute)Attribute.GetCustomAttribute(propNome, typeof(System.ComponentModel.DataAnnotations.RequiredAttribute))!;
            Assert.AreEqual("Nome da Área de Interesse é obrigatório", reqNome.ErrorMessage);
        }
        private static Areainteresse GetTargetAreaInteresse()
        {
            return new Areainteresse
            {
                Id = 1,
                Nome = "Curso",
            };
        }

        private AreaInteresseModel GetTargetAreaInteresseModel()
        {
            return new AreaInteresseModel
            {
                Id = 1,
                Nome = "Curso",
            };
        }

        private IEnumerable<Areainteresse> GetTestAreaInteresses()
        {
            return new List<Areainteresse>
            {
                new Areainteresse
                {
                    Id = 1,
                    Nome = "Curso",
                    },
                new Areainteresse
                {
                        Id = 2,
                        Nome = "Palestra",
                    },
                new Areainteresse
                {
                        Id = 3,
                        Nome = "Mini curso",
                    },
            };
        }
    }
}