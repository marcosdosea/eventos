using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class InscricaoControllerTests
    {
        private const string UsernameTeste = "participante@teste.com";
        private static InscricaoController controller = null!;

        [TestInitialize]
        public void Initialize()
        {
            // Arrange
            var mockService = new Mock<IInscricaoService>();
            mockService.Setup(service => service.GetAllEventsByUserId(UsernameTeste))
                .Returns(GetTestInscricoes());

            controller = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
                Mock.Of<IEventoService>(),
                Mock.Of<IMapper>(),
                mockService.Object,
                Mock.Of<IPessoaService>(),
                Mock.Of<ISubeventoService>());

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, UsernameTeste)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");

            // Injeta o usuário fictício dentro do contexto do Controller
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        [TestMethod()]
        public async Task MinhasInscricoesTest_MapsValorTotal()
        {
            // Act
            var result = await controller.minhasInscricoes(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<InscricaoEventoModel>));

            List<InscricaoEventoModel>? lista = (List<InscricaoEventoModel>)viewResult.ViewData.Model;
            Assert.AreEqual(2, lista.Count);
            Assert.AreEqual(150.00m, lista[0].ValorTotal);
            Assert.AreEqual(0m, lista[1].ValorTotal);
        }

        [TestMethod()]
        public async Task MinhasInscricoesTest_UsesEventoIdFromFirstInscricaoWhenNotProvided()
        {
            // Act
            var result = await controller.minhasInscricoes(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual((uint)1, controller.ViewBag.EventoId);
        }

        private static IEnumerable<Inscricaopessoaevento> GetTestInscricoes()
        {
            return new List<Inscricaopessoaevento>
            {
                new Inscricaopessoaevento
                {
                    Id = 1,
                    IdPessoa = 1,
                    IdEvento = 1,
                    IdPapel = 4,
                    DataInscricao = new DateTime(2024, 09, 02, 07, 30, 0),
                    ValorTotal = 150.00m,
                    Status = "A",
                    FrequenciaFinal = 0m,
                    NomeCracha = "Participante Teste",
                    IdEventoNavigation = new Evento { Id = 1, Nome = "SEMINFO" }
                },
                new Inscricaopessoaevento
                {
                    Id = 2,
                    IdPessoa = 1,
                    IdEvento = 3,
                    IdPapel = 4,
                    DataInscricao = new DateTime(2024, 09, 03, 07, 30, 0),
                    ValorTotal = 0m,
                    Status = "S",
                    FrequenciaFinal = 0m,
                    NomeCracha = "Participante Teste",
                    IdEventoNavigation = new Evento { Id = 3, Nome = "SEMAC" }
                }
            };
        }
    }
}
