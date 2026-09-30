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

            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
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
            var (postController, criadas, criadasSub) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" },
                { "QuantidadeTipoInscricaoSubevento_10_2", "1" }
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
            Assert.AreEqual(100m, criadas[0].ValorTotal);
            Assert.AreEqual("S", criadas[0].Status);
            Assert.AreEqual(1, criadasSub.Count);
            Assert.AreEqual(25m, criadasSub[0].Valor);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_GratuitousTipo_SavesZero()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 3, new Tipoinscricao { Id = 3, IdEvento = 1, Nome = "Gratuita", Valor = 0m } }
            };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_3", "1" }
            });

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
            var (postController, criadas, _) = CreatePostController(
                new Dictionary<uint, Tipoinscricao>(),
                new Dictionary<string, string>(),
                new Evento { Id = 1, Status = "A", PossuiCertificado = 1, InscricaoGratuita = 0, ValorInscricao = 100m });

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
            var (postController, criadas, _) = CreatePostController(
                new Dictionary<uint, Tipoinscricao>(),
                new Dictionary<string, string>(),
                new Evento { Id = 1, Status = "A", PossuiCertificado = 1, InscricaoGratuita = 1, ValorInscricao = 0m });

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

        private static (InscricaoController, List<Inscricaopessoaevento>, List<Inscricaopessoasubevento>) CreatePostController(
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
            var criadasSub = new List<Inscricaopessoasubevento>();
            mockInscricaoService.Setup(service => service.CreateInscricaoSubEvento(It.IsAny<Inscricaopessoasubevento>()))
                .Callback<Inscricaopessoasubevento>(i => criadasSub.Add(i));

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns<uint>(id => tipos.TryGetValue(id, out var tipo) ? tipo : null);

            evento ??= new Evento { Id = 1, Status = "A", PossuiCertificado = 1 };
            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns(evento);

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(service => service.Get(10))
                .Returns(new Subevento { Id = 10, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1), ValorInscricao = 50m });

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();

            var postController = new InscricaoController(
                null!,
                mockTipoService.Object,
                mockEventoService.Object,
                mapper,
                mockInscricaoService.Object,
                mockPessoaService.Object,
                mockSubeventoService.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, UsernameTeste) };
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"))
            };
            httpContext.Request.Form = new FormCollection(
                formFields.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value)));
            postController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            postController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            return (postController, criadas, criadasSub);
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
                    IdEventoNavigation = new Evento { Id = 1, Nome = "SEMINFO" },
                    Inscricaopessoasubeventos = new List<Inscricaopessoasubevento>
                    {
                        new Inscricaopessoasubevento
                        {
                            IdPessoa = 1,
                            IdSubEvento = 10,
                            IdPapel = 4,
                            DataInscricao = new DateTime(2024, 09, 02, 08, 0, 0),
                            Valor = 30m,
                            Status = "A",
                            FrequenciaFinal = 100m,
                            IdSubEventoNavigation = new Subevento { Id = 10, Nome = "Minicurso Docker", IdEvento = 1 }
                        }
                    }
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

        [TestMethod()]
        public async Task MinhasInscricoesTest_LoadsSubEventsForEvent()
        {
            // Act
            var result = await controller.minhasInscricoes(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            var lista = (List<InscricaoEventoModel>)viewResult.ViewData.Model!;
            Assert.AreEqual(2, lista.Count);

            var firstEvento = lista[0];
            Assert.IsNotNull(firstEvento.Inscricaopessoasubeventos);
            Assert.AreEqual(1, firstEvento.Inscricaopessoasubeventos.Count);

            var subItem = firstEvento.Inscricaopessoasubeventos.First();
            Assert.AreEqual((uint)10, subItem.IdSubEvento);
            Assert.AreEqual(30m, subItem.Valor);
            Assert.AreEqual(100m, subItem.FrequenciaFinal);
            Assert.AreEqual("Minicurso Docker", subItem.IdSubEventoNavigation?.Nome);
        }

        [TestMethod()]
        public async Task DetalhesInscricaoTest_ValidId_ReturnsViewWithInscricaoDetails()
        {
            // Act
            var result = await controller.DetalhesInscricao(1, null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(InscricaoEventoModel));

            var model = (InscricaoEventoModel)viewResult.ViewData.Model!;
            Assert.AreEqual((uint)1, model.Id);
            Assert.AreEqual(150m, model.ValorTotal);
            Assert.AreEqual("A", model.Status);
            Assert.IsNotNull(model.Inscricaopessoasubeventos);
            Assert.AreEqual(1, model.Inscricaopessoasubeventos.Count);
            Assert.AreEqual("Minicurso Docker", model.Inscricaopessoasubeventos.First().IdSubEventoNavigation?.Nome);
        }

        [TestMethod()]
        public async Task DetalhesInscricaoTest_ValidEventoId_ReturnsViewWithInscricaoDetails()
        {
            // Act - fallback buscando por IdEvento quando id for nulo
            var result = await controller.DetalhesInscricao(null, 3);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            var model = (InscricaoEventoModel)viewResult.ViewData.Model!;
            Assert.AreEqual((uint)2, model.Id);
            Assert.AreEqual((uint)3, model.IdEvento);
            Assert.AreEqual("SEMAC", model.IdEventoNavigation?.Nome);
        }

        [TestMethod()]
        public async Task DetalhesInscricaoTest_NotFound_RedirectsToMinhasInscricoes()
        {
            // Act - id inexistente
            var result = await controller.DetalhesInscricao(999, null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("minhasInscricoes", redirect.ActionName);
            Assert.AreEqual("Inscrição não encontrada.", controller.TempData["ParticipanteMessage"]);
        }

        [TestMethod()]
        public async Task MinhasInscricoesTest_UnauthenticatedUser_RedirectsToHomeIndex()
        {
            // Arrange - contexto com identidade anônima / sem nome
            var unauthContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            var originalContext = controller.ControllerContext;
            controller.ControllerContext = new ControllerContext { HttpContext = unauthContext };

            try
            {
                // Act
                var result = await controller.minhasInscricoes(null);

                // Assert
                Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
                var redirect = (RedirectToActionResult)result;
                Assert.AreEqual("Index", redirect.ActionName);
                Assert.AreEqual("Home", redirect.ControllerName);
            }
            finally
            {
                controller.ControllerContext = originalContext;
            }
        }

        [TestMethod()]
        public async Task DetalhesInscricaoTest_UnauthenticatedUser_RedirectsToHomeIndex()
        {
            // Arrange - contexto com identidade anônima / sem nome
            var unauthContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            var originalContext = controller.ControllerContext;
            controller.ControllerContext = new ControllerContext { HttpContext = unauthContext };

            try
            {
                // Act
                var result = await controller.DetalhesInscricao(1, null);

                // Assert
                Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
                var redirect = (RedirectToActionResult)result;
                Assert.AreEqual("Index", redirect.ActionName);
                Assert.AreEqual("Home", redirect.ControllerName);
            }
            finally
            {
                controller.ControllerContext = originalContext;
            }
        }
    }
}
