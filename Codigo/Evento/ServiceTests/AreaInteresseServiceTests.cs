using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace Service.Tests
{
    [TestClass()]
    public class AreaInteresseServiceTests
    {

        private EventoContext? context;
        private IAreaInteresseService? areaInteresseService;

        [TestInitialize]
        public void Initialize()
        {
            //Arrange
            var builder = new DbContextOptionsBuilder<EventoContext>();
            builder.UseInMemoryDatabase("Evento");
            var options = builder.Options;

            context = new EventoContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            var listaAreaInteresse = new List<Areainteresse>
            {
                new Areainteresse
                {
                    Id = 1,
                    Nome = "Shows Musicais"
                },
                new Areainteresse
                {
                    Id = 2,
                    Nome = "Tecnologia"
                }
            };

            context.AddRange(listaAreaInteresse);
            context.SaveChanges();

            areaInteresseService = new AreaInteresseService(context);
        }

        [TestMethod()]
        public void CreateValidTest()
        {
            // Act
            areaInteresseService.Create(
                new Areainteresse
                {
                    Id = 3,
                    Nome = "Saúde"
                });

            // Assert
            var resultList = areaInteresseService.GetAll();
            var listaAreasInteresse = resultList.ToImmutableList();
            Assert.AreEqual(3, listaAreasInteresse.Count());

            var areaInteresse = areaInteresseService.Get(3);

            Assert.AreEqual((uint)3, areaInteresse.Id);
            Assert.AreEqual("Saúde", areaInteresse.Nome);
        }
    }
}