using AutoMapper;
using Core;
using Core.DTO;
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

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_UsesPessoaNomeOrNomeCracha_NotLoginOrCpf()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } }
            };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            });

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 100m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual("Participante Teste", criadas[0].NomeCracha);
            Assert.AreNotEqual(UsernameTeste, criadas[0].NomeCracha);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_PrefersCustomNomeCrachaFromForm()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } }
            };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            });

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 100m,
                NomeCracha = "Dr. Teste"
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual("Dr. Teste", criadas[0].NomeCracha);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_TruncatesNomeCrachaTo20Characters()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } }
            };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            });

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 100m,
                NomeCracha = "Nome Muito Longo Que Ultrapassa Vinte Caracteres"
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual("Nome Muito Longo Que", criadas[0].NomeCracha);
            Assert.AreEqual(20, criadas[0].NomeCracha!.Length);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_UsesPessoaNomeCrachaWhenPresent()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } }
            };
            var pessoaComCracha = new Pessoa
            {
                Id = 1,
                Cpf = UsernameTeste,
                Nome = "Nome Completo do Participante",
                NomeCracha = "Apelido Crachá"
            };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            }, pessoa: pessoaComCracha);

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 100m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual("Apelido Crachá", criadas[0].NomeCracha);
        }

        [TestMethod()]
        public void RealizarInscricaoTest_Get_PrepopulatesNomeCrachaInViewModel()
        {
            // Arrange
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Nome Longo Participante", NomeCracha = "Apelido" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1))
                .Returns(new Evento { Id = 1, Status = "A", PossuiCertificado = 1, Nome = "Evento Teste" });

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(s => s.GetByEvento(1)).Returns(new List<Tipoinscricao>());

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.GetByIdEvento(1)).Returns(new List<SubeventoEventoDTO>());

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.GetGestorInEvent(UsernameTeste, 1)).Returns((Inscricaopessoaevento)null!);
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(false);

            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new EventoProfile());
                cfg.AddProfile(new InscricaoProfile());
            }).CreateMapper();

            var getController = new InscricaoController(
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
            getController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            getController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            getController.Url = Mock.Of<IUrlHelper>();

            // Act
            var result = getController.realizarInscricao(1, (uint?)null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.Model, typeof(InscricaoEventoViewModel));
            var model = (InscricaoEventoViewModel)viewResult.Model;
            Assert.IsNotNull(model.inscricaoNavigation);
            Assert.AreEqual("Apelido", model.inscricaoNavigation.NomeCracha);
        }

        [TestMethod()]
        public void RealizarInscricaoTest_Get_PrepopulatesNomeCrachaFromExistingInscricaoWhenAlreadyInscribed()
        {
            // Arrange
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Nome Completo", NomeCracha = "Apelido" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1))
                .Returns(new Evento { Id = 1, Status = "A", PossuiCertificado = 1, Nome = "Evento Teste" });

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(s => s.GetByEvento(1)).Returns(new List<Tipoinscricao>());

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.GetByIdEvento(1)).Returns(new List<SubeventoEventoDTO>());

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.GetGestorInEvent(UsernameTeste, 1)).Returns((Inscricaopessoaevento)null!);
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetByEvento(1)).Returns(new List<Inscricaopessoaevento>
            {
                new Inscricaopessoaevento
                {
                    Id = 1,
                    IdPessoa = 1,
                    IdEvento = 1,
                    IdPapel = 4,
                    NomeCracha = "Cracha Personalizado"
                }
            });

            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new EventoProfile());
                cfg.AddProfile(new InscricaoProfile());
            }).CreateMapper();

            var getController = new InscricaoController(
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
            getController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            getController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            getController.Url = Mock.Of<IUrlHelper>();

            // Act
            var result = getController.realizarInscricao(1, (uint?)null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            var model = (InscricaoEventoViewModel)viewResult.Model;
            Assert.IsNotNull(model.inscricaoNavigation);
            Assert.AreEqual("Cracha Personalizado", model.inscricaoNavigation.NomeCracha);
            Assert.AreEqual(true, getController.ViewBag.JaInscrito);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_UpdatesPessoaNomeCrachaWhenChanged()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } }
            };
            var pessoa = new Pessoa
            {
                Id = 1,
                Cpf = UsernameTeste,
                Nome = "Jordan Teste",
                NomeCracha = "Jordan"
            };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            }, pessoa: pessoa);

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 100m,
                NomeCracha = "Jordan xxxxxxxxxxxxx"
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual("Jordan xxxxxxxxxxxxx", criadas[0].NomeCracha);
            Assert.AreEqual("Jordan xxxxxxxxxxxxx", pessoa.NomeCracha);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_SubEventTicketsExceedsLimit_BlocksRegistrationAndSetsWarning()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } },
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "VIP Sub", Valor = 25m } }
            };
            var (postController, criadas, criadasSub) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" },
                { "QuantidadeTipoInscricaoSubevento_10_2", "10" }
            });

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint> { 10 },
                ValorTotal = 350m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("Limite máximo para subeventos excedido.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count, "Nenhum ingresso de evento principal deve ser criado.");
            Assert.AreEqual(0, criadasSub.Count, "Nenhum ingresso de subevento deve ser criado.");
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_MultipleSubevents_OneExceedsLimit_BlocksEntireRegistration()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Paga", Valor = 100m } },
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "VIP Sub 1", Valor = 25m } },
                { 3, new Tipoinscricao { Id = 3, IdEvento = 1, Nome = "VIP Sub 2", Valor = 30m } }
            };

            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(service => service.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockInscricaoService = new Mock<IInscricaoService>();
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

            var evento = new Evento { Id = 1, Status = "A", PossuiCertificado = 0 };
            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns(evento);

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(service => service.Get(10))
                .Returns(new Subevento { Id = 10, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1) });
            mockSubeventoService.Setup(service => service.Get(20))
                .Returns(new Subevento { Id = 20, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1) });

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

            var formFields = new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "2" },
                { "QuantidadeTipoInscricaoSubevento_10_2", "3" },
                { "QuantidadeTipoInscricaoSubevento_20_3", "9" } // Excede 8
            };
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, UsernameTeste) }, "TestAuthType"))
            };
            httpContext.Request.Form = new FormCollection(
                formFields.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value)));
            postController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            postController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint> { 10, 20 },
                ValorTotal = 500m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("Limite máximo para subeventos excedido.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count, "Nenhum ingresso do evento principal deve ser persistido.");
            Assert.AreEqual(0, criadasSub.Count, "Nenhum ingresso de subevento deve ser persistido.");
        }

        private static (InscricaoController, List<Inscricaopessoaevento>, List<Inscricaopessoasubevento>) CreatePostController(
            Dictionary<uint, Tipoinscricao> tipos,
            Dictionary<string, string> formFields,
            Evento? evento = null,
            Pessoa? pessoa = null)
        {
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(service => service.GetByCpf(UsernameTeste))
                .Returns(pessoa ?? new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

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
