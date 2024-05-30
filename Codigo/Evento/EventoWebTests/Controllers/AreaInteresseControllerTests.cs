using Microsoft.VisualStudio.TestTools.UnitTesting;
using EventoWeb.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Core.Service;
using Moq;
using EventoWeb.Mappers;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;

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

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new AreainteresseProfile())).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestAreasInteresse());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetAreaInteresse());
            mockService.Setup(service => service.Edit(It.IsAny<Areainteresse>()))
                .Verifiable();
            mockService.Setup(service => service.Create(It.IsAny<Areainteresse>()))
                .Verifiable();
            controller = new AreaInteresseController(mockService.Object, mapper);
        }

        [TestMethod()]
        public void IndexTest()
        {
            // Act
            var result = controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<AreaInteresseModel>));

            List<AreaInteresseModel>? lista = (List<AreaInteresseModel>) viewResult.ViewData.Model;
            Assert.AreEqual(2, lista.Count);
        }

        private static Areainteresse GetTargetAreaInteresse()
        {
            return new Areainteresse
            {
                Id = 1,
                Nome = "Show Musical"
            };
        }
        private IEnumerable<Areainteresse> GetTestAreasInteresse()
        {
            return new List<Areainteresse>
            {
                new Areainteresse
                {
                    Id = 1,
                    Nome = "Show Musical",
                },
                new Areainteresse
                {
                    Id = 1,
                    Nome = "Tecnologia",
                },
            };
        }
    }
}