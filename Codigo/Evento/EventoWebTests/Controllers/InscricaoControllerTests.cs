using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
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

            IMapper getMapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();

            controller = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
                Mock.Of<IEventoService>(),
                getMapper,
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

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_CalculatesValorTotalOnServer()
        {
            // Arrange: tipo do evento (100) + tipo do subevento (25).
            // O form nao posta ValorTotal, entao envia um valor adulterado (999)
            // para provar que o servidor ignora o cliente.
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } },
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "VIP Sub", Valor = 25m } }
            };
            var (postController, criadas) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "TipoInscricaoSubevento_10", "2" }
            });

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint> { 10 },
                ValorTotal = 999m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual((uint)1, criadas[0].IdTipoInscricao);
            Assert.AreEqual(125m, criadas[0].ValorTotal);
            Assert.AreEqual("S", criadas[0].Status);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_GratuitousTipo_SavesZero()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 3, new Tipoinscricao { Id = 3, IdEvento = 1, Nome = "Gratuita", Valor = 0m } }
            };
            var (postController, criadas) = CreatePostController(tipos, new Dictionary<string, string>());

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 3,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 50m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual(0m, criadas[0].ValorTotal);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_NoTiposConfigured_UsesEventDefaultPrice()
        {
            // Arrange: evento pago sem tipos configurados posta IdTipoInscricao = 0;
            // a tela informa que "a inscrição padrão será aplicada".
            var (postController, criadas) = CreatePostController(
                new Dictionary<uint, Tipoinscricao>(),
                new Dictionary<string, string>(),
                new Evento { Id = 1, InscricaoGratuita = 0, ValorInscricao = 100m });

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 0,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 0m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual(100m, criadas[0].ValorTotal);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_NoTiposConfigured_GratuitousEvent_SavesZero()
        {
            // Arrange
            var (postController, criadas) = CreatePostController(
                new Dictionary<uint, Tipoinscricao>(),
                new Dictionary<string, string>(),
                new Evento { Id = 1, InscricaoGratuita = 1, ValorInscricao = 0m });

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 0,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 0m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual(0m, criadas[0].ValorTotal);
        }

        private static (InscricaoController, List<Inscricaopessoaevento>) CreatePostController(
            Dictionary<uint, Tipoinscricao> tipos,
            Dictionary<string, string> formFields,
            Evento? evento = null)
        {
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(service => service.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(service => service.IsInscrito(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns(false);
            var criadas = new List<Inscricaopessoaevento>();
            mockInscricaoService.Setup(service => service.CreateInscricaoEvento(It.IsAny<Inscricaopessoaevento>()))
                .Callback<Inscricaopessoaevento>(i => criadas.Add(i))
                .Returns((uint)1);

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns<uint>(id => tipos.TryGetValue(id, out var tipo) ? tipo : null);

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns(evento);

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();

            var postController = new InscricaoController(
                null!,
                mockTipoService.Object,
                mockEventoService.Object,
                mapper,
                mockInscricaoService.Object,
                mockPessoaService.Object,
                Mock.Of<ISubeventoService>());

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, UsernameTeste) };
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"))
            };
            httpContext.Request.Form = new FormCollection(
                formFields.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value)));
            postController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            postController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            return (postController, criadas);
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
