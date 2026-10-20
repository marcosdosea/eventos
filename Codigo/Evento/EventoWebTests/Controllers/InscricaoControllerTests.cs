using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
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
        public async Task RealizarInscricaoTest_Post_TipoDeOutroEvento_RejeitaSemSalvar()
        {
            // Arrange (#780): tipo 9 pertence ao evento 2, mas o form forja
            // QuantidadeTipoInscricao_9 na inscrição do evento 1.
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 9, new Tipoinscricao { Id = 9, IdEvento = 2, Nome = "Barato Outro Evento", Valor = 0m } }
            };
            var (postController, criadas, criadasSub) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_9", "1" }
            });

            var input = new InscricaoEventoModel
            {
                SelectedSubeventos = new List<uint>(),
                ValorTotal = 0m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("realizarInscricao", redirect.ActionName);
            Assert.AreEqual("Tipo de inscrição inválido para este evento.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count);
            Assert.AreEqual(0, criadasSub.Count);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_MeiaEntrada_PersisteTipoNuloEMetadeDoValor()
        {
            // Arrange: posta o sentinela de meia entrada tanto para o evento
            // principal (valor 100 -> 50) quanto para o subevento 10 (valor 50 -> 25).
            // Deve persistir IdTipoInscricao = null em ambos.
            var evento = new Evento { Id = 1, Status = "A", PossuiCertificado = 1, ValorInscricao = 100m, VagasDisponiveis = 100 };
            var meia = InscricaoController.TIPO_MEIA_ENTRADA;
            var (postController, criadas, criadasSub) = CreatePostController(
                new Dictionary<uint, Tipoinscricao>(),
                new Dictionary<string, string>
                {
                    { $"QuantidadeTipoInscricao_{meia}", "1" },
                    { $"QuantidadeTipoInscricaoSubevento_10_{meia}", "1" }
                },
                evento);

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = meia,
                SelectedSubeventos = new List<uint> { 10 },
                ValorTotal = 999m
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.IsNull(criadas[0].IdTipoInscricao, "Meia entrada nao e um tipo real: deve gravar IdTipoInscricao = null.");
            Assert.AreEqual(50m, criadas[0].ValorTotal, "Evento com meia entrada deve custar 50% do valor.");

            Assert.AreEqual(1, criadasSub.Count);
            Assert.IsNull(criadasSub[0].IdTipoInscricao, "Meia entrada no subevento tambem deve gravar IdTipoInscricao = null.");
            Assert.AreEqual(25m, criadasSub[0].Valor, "Subevento com meia entrada deve custar 50% do valor.");
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
                new Evento { Id = 1, Status = "A", PossuiCertificado = 1, InscricaoGratuita = 0, ValorInscricao = 100m, VagasDisponiveis = 100 });

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
                new Evento { Id = 1, Status = "A", PossuiCertificado = 1, InscricaoGratuita = 1, ValorInscricao = 0m, VagasDisponiveis = 100 });

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
        public async Task RealizarInscricaoTest_Post_UtilizaNomeOuNomeCrachaDaPessoa_NaoLoginOuCpf()
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
        public async Task RealizarInscricaoTest_Post_PriorizaNomeCrachaPersonalizadoDoFormulario()
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
        public async Task RealizarInscricaoTest_Post_TruncaNomeCrachaEm20Caracteres()
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
        public async Task RealizarInscricaoTest_Post_UtilizaNomeCrachaDaPessoaQuandoPresente()
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
        public void RealizarInscricaoTest_Get_PreencheNomeCrachaNoViewModel()
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
        public void RealizarInscricaoTest_Get_PreencheNomeCrachaDaInscricaoExistenteQuandoJaInscrito()
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
        public async Task RealizarInscricaoTest_Post_AtualizaNomeCrachaDaPessoaQuandoAlterado()
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

            var evento = new Evento { Id = 1, Status = "A", PossuiCertificado = 0, VagasDisponiveis = 100 };
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

        [TestMethod()]
        public void RealizarInscricaoTest_Get_PeriodoInscricaoNaoIniciado_RedirecionaParaHomeComMensagem()
        {
            // Arrange
            var evento = new Evento
            {
                Id = 1,
                Status = "A",
                PossuiCertificado = 1,
                DataInicioInscricao = DateTime.Now.AddDays(2)
            };
            var controller = CreateGetController(evento);

            // Act
            var result = controller.realizarInscricao(1, (uint?)null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("O período de inscrições para este evento ainda não começou.", controller.TempData["ParticipanteMessage"]);
        }

        [TestMethod()]
        public void RealizarInscricaoTest_Get_PeriodoInscricaoEncerrado_RedirecionaParaHomeComMensagem()
        {
            // Arrange
            var evento = new Evento
            {
                Id = 1,
                Status = "A",
                PossuiCertificado = 1,
                DataFimInscricao = DateTime.Now.AddDays(-1)
            };
            var controller = CreateGetController(evento);

            // Act
            var result = controller.realizarInscricao(1, (uint?)null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("O período de inscrições para este evento já foi encerrado.", controller.TempData["ParticipanteMessage"]);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_LotacaoEsgotada_BloqueiaERedireciona()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } }
            };
            var evento = new Evento { Id = 1, Status = "A", PossuiCertificado = 1, VagasDisponiveis = 0 };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            }, evento);

            var input = new InscricaoEventoModel { IdTipoInscricao = 1 };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("Lotação esgotada.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_PeriodoInscricaoNaoIniciado_BloqueiaERedireciona()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } }
            };
            var evento = new Evento { Id = 1, Status = "A", PossuiCertificado = 1, VagasDisponiveis = 100, DataInicioInscricao = DateTime.Now.AddDays(2) };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            }, evento);

            var input = new InscricaoEventoModel { IdTipoInscricao = 1 };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("O período de inscrições para este evento ainda não começou.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_PeriodoInscricaoEncerrado_BloqueiaERedireciona()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } }
            };
            var evento = new Evento { Id = 1, Status = "A", PossuiCertificado = 1, VagasDisponiveis = 100, DataFimInscricao = DateTime.Now.AddDays(-1) };
            var (postController, criadas, _) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            }, evento);

            var input = new InscricaoEventoModel { IdTipoInscricao = 1 };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("O período de inscrições para este evento já foi encerrado.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_SubeventoEsgotado_BloqueiaERedireciona()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } }
            };
            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.Get(10))
                .Returns(new Subevento
                {
                    Id = 10,
                    IdEvento = 1,
                    Nome = "Minicurso Esgotado",
                    Status = "A",
                    DataFimInscricao = DateTime.Now.AddDays(1),
                    VagasDisponiveis = 0
                });

            var (postController, criadas, criadasSub) = CreatePostController(
                tipos,
                new Dictionary<string, string> { { "QuantidadeTipoInscricao_1", "1" } },
                customSubeventoService: mockSubeventoService);

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint> { 10 }
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("realizarInscricao", redirect.ActionName);
            Assert.AreEqual("As vagas para o subevento Minicurso Esgotado estão esgotadas.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count);
            Assert.AreEqual(0, criadasSub.Count);
        }

        [TestMethod()]
        public void RealizarInscricaoTest_Get_UsuarioJaInscritoNoEventoPrincipal_MasComSubeventosDisponiveis_PermiteAcessoETrazFlags()
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
            mockTipoService.Setup(s => s.GetTiposAgrupadosPorSubevento(1)).Returns(new Dictionary<uint, List<TipoInscricaoDTO>>());

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.GetByIdEvento(1)).Returns(new List<SubeventoEventoDTO>
            {
                new SubeventoEventoDTO { Id = 10, Nome = "Subevento 1", Status = "A" },
                new SubeventoEventoDTO { Id = 20, Nome = "Subevento 2", Status = "A" }
            });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.GetGestorInEvent(UsernameTeste, 1)).Returns((Inscricaopessoaevento)null!);
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetByEvento(1)).Returns(new List<Inscricaopessoaevento>
            {
                new Inscricaopessoaevento { Id = 1, IdPessoa = 1, IdEvento = 1, IdPapel = 4 }
            });
            mockInscricaoService.Setup(s => s.GetSubByEvento(1)).Returns(new List<Inscricaopessoasubevento>
            {
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, Status = "A" }
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
            Assert.AreEqual(true, getController.ViewBag.JaInscritoEventoPrincipal);
            Assert.IsNull(getController.ViewBag.JaInscrito);
            var subInscritos = getController.ViewBag.SubeventosInscritos as HashSet<uint>;
            Assert.IsNotNull(subInscritos);
            Assert.IsTrue(subInscritos.Contains(10));
            Assert.IsFalse(subInscritos.Contains(20));
        }

        [TestMethod()]
        public void RealizarInscricaoTest_Get_UsuarioJaInscritoNoEventoPrincipalETodosSubeventos_BloqueiaComJaInscrito()
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
            mockTipoService.Setup(s => s.GetTiposAgrupadosPorSubevento(1)).Returns(new Dictionary<uint, List<TipoInscricaoDTO>>());

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.GetByIdEvento(1)).Returns(new List<SubeventoEventoDTO>
            {
                new SubeventoEventoDTO { Id = 10, Nome = "Subevento 1", Status = "A" }
            });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.GetGestorInEvent(UsernameTeste, 1)).Returns((Inscricaopessoaevento)null!);
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetByEvento(1)).Returns(new List<Inscricaopessoaevento>
            {
                new Inscricaopessoaevento { Id = 1, IdPessoa = 1, IdEvento = 1, IdPapel = 4 }
            });
            mockInscricaoService.Setup(s => s.GetSubByEvento(1)).Returns(new List<Inscricaopessoasubevento>
            {
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, Status = "A" }
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
            Assert.AreEqual(true, getController.ViewBag.JaInscritoEventoPrincipal);
            Assert.AreEqual(true, getController.ViewBag.JaInscrito);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_UsuarioJaInscritoNoEventoPrincipal_InscreveApenasEmNovosSubeventos()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } },
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "Sub Tipo", Valor = 20m } }
            };

            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1))
                .Returns(new Evento { Id = 1, Status = "A", PossuiCertificado = 1, VagasDisponiveis = 100 });

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(s => s.Get(It.IsAny<uint>()))
                .Returns<uint>(id => tipos.TryGetValue(id, out var t) ? t : null);

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.Get(10))
                .Returns(new Subevento { Id = 10, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1), VagasDisponiveis = 100 });
            mockSubeventoService.Setup(s => s.Get(20))
                .Returns(new Subevento { Id = 20, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1), VagasDisponiveis = 100 });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetSubByEvento(1)).Returns(new List<Inscricaopessoasubevento>
            {
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, Status = "A" }
            });

            var criadasEvento = new List<Inscricaopessoaevento>();
            var criadasSub = new List<Inscricaopessoasubevento>();
            mockInscricaoService.Setup(s => s.CreateInscricoesEmLote(
                    It.IsAny<IEnumerable<Inscricaopessoaevento>>(),
                    It.IsAny<IEnumerable<Inscricaopessoasubevento>>(),
                    It.IsAny<bool>()))
                .Callback<IEnumerable<Inscricaopessoaevento>, IEnumerable<Inscricaopessoasubevento>, bool>((e, s, _) =>
                {
                    if (e != null) criadasEvento.AddRange(e);
                    if (s != null) criadasSub.AddRange(s);
                });

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();

            var controller = new InscricaoController(
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
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "QuantidadeTipoInscricaoSubevento_20_2", "1" }
            });
            httpContext.Request.Form = formCollection;

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var input = new InscricaoEventoModel
            {
                SelectedSubeventos = new List<uint> { 10, 20 }
            };

            // Act
            var result = await controller.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("minhasInscricoes", redirect.ActionName);
            Assert.AreEqual("Inscrição no(s) subevento(s) realizada com sucesso!", controller.TempData["ParticipanteSuccessMessage"]);
            Assert.AreEqual(0, criadasEvento.Count, "Não deve duplicar a inscrição no evento principal.");
            Assert.AreEqual(1, criadasSub.Count, "Deve inscrever apenas no novo subevento.");
            Assert.AreEqual((uint)20, criadasSub[0].IdSubEvento);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_UsuarioJaInscritoNoEventoPrincipal_SemNovosSubeventos_RedirecionaComMensagemJaInscrito()
        {
            // Arrange
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1))
                .Returns(new Evento { Id = 1, Status = "A", PossuiCertificado = 1, VagasDisponiveis = 100 });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetSubByEvento(1)).Returns(new List<Inscricaopessoasubevento>
            {
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, Status = "A" }
            });

            var controller = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
                mockEventoService.Object,
                Mock.Of<IMapper>(),
                mockInscricaoService.Object,
                mockPessoaService.Object,
                Mock.Of<ISubeventoService>());

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, UsernameTeste) };
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"))
            };
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var input = new InscricaoEventoModel
            {
                SelectedSubeventos = new List<uint> { 10 }
            };

            // Act
            var result = await controller.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("minhasInscricoes", redirect.ActionName);
            Assert.AreEqual("Você já está inscrito neste evento!", controller.TempData["ParticipanteMessage"]);
        }

        [TestMethod()]
        public void RealizarInscricaoTest_Get_StatusInativo_RedirecionaParaHomeComMensagem()
        {
            // Arrange
            var evento = new Evento
            {
                Id = 1,
                Status = "I",
                PossuiCertificado = 1
            };
            var getController = CreateGetController(evento);

            // Act
            var result = getController.realizarInscricao(1, (uint?)null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("true", redirect.RouteValues?["vitrine"]);
            Assert.AreEqual("Este evento não está ativo para inscrições.", getController.TempData["ParticipanteMessage"]);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_StatusInativo_BloqueiaERedireciona()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } }
            };
            var evento = new Evento { Id = 1, Status = "I", PossuiCertificado = 1, VagasDisponiveis = 100 };
            var (postController, criadas, criadasSub) = CreatePostController(tipos, new Dictionary<string, string>
            {
                { "QuantidadeTipoInscricao_1", "1" }
            }, evento);

            var input = new InscricaoEventoModel { IdTipoInscricao = 1 };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Home", redirect.ControllerName);
            Assert.AreEqual("Este evento não está ativo para inscrições.", postController.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadas.Count);
            Assert.AreEqual(0, criadasSub.Count);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_SubeventoStatusInativo_NaoCriaInscricaoSub()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } },
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "Sub Inativo", Valor = 15m } }
            };
            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.Get(10))
                .Returns(new Subevento
                {
                    Id = 10,
                    IdEvento = 1,
                    Nome = "Minicurso Inativo",
                    Status = "I",
                    DataFimInscricao = DateTime.Now.AddDays(1),
                    VagasDisponiveis = 10
                });

            var (postController, criadas, criadasSub) = CreatePostController(
                tipos,
                new Dictionary<string, string>
                {
                    { "QuantidadeTipoInscricao_1", "1" },
                    { "QuantidadeTipoInscricaoSubevento_10_2", "1" }
                },
                customSubeventoService: mockSubeventoService);

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint> { 10 }
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count, "Inscrição no evento principal deve ser criada.");
            Assert.AreEqual(0, criadasSub.Count, "Subevento inativo (Status 'I') não deve gerar inscrição.");
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_EventoSemCertificado_UsuarioJaInscrito_PermiteComprarIngressosAdicionais()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Lote Principal", Valor = 10m } },
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "Sub Tipo Lote", Valor = 20m } }
            };

            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1))
                .Returns(new Evento { Id = 1, Status = "A", PossuiCertificado = 0, VagasDisponiveis = 100, ValorInscricao = 10m });

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(s => s.Get(It.IsAny<uint>()))
                .Returns<uint>(id => tipos.TryGetValue(id, out var t) ? t : null);

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.Get(10))
                .Returns(new Subevento { Id = 10, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1), VagasDisponiveis = 100 });
            mockSubeventoService.Setup(s => s.Get(20))
                .Returns(new Subevento { Id = 20, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1), VagasDisponiveis = 100 });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetSubByEvento(1)).Returns(new List<Inscricaopessoasubevento>
            {
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, Status = "A" }
            });

            var criadasEvento = new List<Inscricaopessoaevento>();
            var criadasSub = new List<Inscricaopessoasubevento>();
            mockInscricaoService.Setup(s => s.CreateInscricoesEmLote(
                    It.IsAny<IEnumerable<Inscricaopessoaevento>>(),
                    It.IsAny<IEnumerable<Inscricaopessoasubevento>>(),
                    It.IsAny<bool>()))
                .Callback<IEnumerable<Inscricaopessoaevento>, IEnumerable<Inscricaopessoasubevento>, bool>((e, s, _) =>
                {
                    if (e != null) criadasEvento.AddRange(e);
                    if (s != null) criadasSub.AddRange(s);
                });

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();

            var controller = new InscricaoController(
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
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "QuantidadeTipoInscricao_1", "2" },
                { "QuantidadeTipoInscricaoSubevento_20_2", "1" }
            });
            httpContext.Request.Form = formCollection;

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var input = new InscricaoEventoModel
            {
                SelectedSubeventos = new List<uint> { 20 }
            };

            // Act
            var result = await controller.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("minhasInscricoes", redirect.ActionName);
            Assert.AreEqual(2, criadasEvento.Count, "Deve permitir compra de ingressos adicionais no evento sem certificado.");
            Assert.AreEqual(1, criadasSub.Count, "Deve inscrever no subevento selecionado.");
            Assert.AreEqual((uint)20, criadasSub[0].IdSubEvento);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_EventoSemCertificado_UsuarioJaInscrito_PermiteMultiplosIngressosMesmoSubevento()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "Sub Tipo Lote", Valor = 20m } }
            };

            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1))
                .Returns(new Evento { Id = 1, Status = "A", PossuiCertificado = 0, VagasDisponiveis = 100, ValorInscricao = 10m });

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(s => s.Get(It.IsAny<uint>()))
                .Returns<uint>(id => tipos.TryGetValue(id, out var t) ? t : null);

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.Get(10))
                .Returns(new Subevento { Id = 10, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1), VagasDisponiveis = 100 });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetSubByEvento(1)).Returns(new List<Inscricaopessoasubevento>
            {
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, Status = "A" }
            });

            var criadasSub = new List<Inscricaopessoasubevento>();
            mockInscricaoService.Setup(s => s.CreateInscricoesEmLote(
                    It.IsAny<IEnumerable<Inscricaopessoaevento>>(),
                    It.IsAny<IEnumerable<Inscricaopessoasubevento>>(),
                    It.IsAny<bool>()))
                .Callback<IEnumerable<Inscricaopessoaevento>, IEnumerable<Inscricaopessoasubevento>, bool>((_, s, _) =>
                {
                    if (s != null) criadasSub.AddRange(s);
                });

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();

            var controller = new InscricaoController(
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
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "QuantidadeTipoInscricaoSubevento_10_2", "2" }
            });
            httpContext.Request.Form = formCollection;

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var input = new InscricaoEventoModel
            {
                SelectedSubeventos = new List<uint> { 10 }
            };

            // Act
            var result = await controller.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(2, criadasSub.Count, "Deve permitir múltiplos ingressos no mesmo subevento.");
            Assert.IsTrue(criadasSub.All(s => s.IdSubEvento == 10));
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_EventoSemCertificado_UsuarioJaInscrito_SemNovosIngressos_BloqueiaDuplicata()
        {
            // Arrange
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(1))
                .Returns(new Evento { Id = 1, Status = "A", PossuiCertificado = 0, VagasDisponiveis = 100 });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.IsInscrito(1, 1)).Returns(true);
            mockInscricaoService.Setup(s => s.GetSubByEvento(1)).Returns(new List<Inscricaopessoasubevento>
            {
                new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 10, Status = "A" }
            });

            var criadasEvento = new List<Inscricaopessoaevento>();
            var criadasSub = new List<Inscricaopessoasubevento>();
            mockInscricaoService.Setup(s => s.CreateInscricoesEmLote(
                    It.IsAny<IEnumerable<Inscricaopessoaevento>>(),
                    It.IsAny<IEnumerable<Inscricaopessoasubevento>>(),
                    It.IsAny<bool>()))
                .Callback<IEnumerable<Inscricaopessoaevento>, IEnumerable<Inscricaopessoasubevento>, bool>((e, s, _) =>
                {
                    if (e != null) criadasEvento.AddRange(e);
                    if (s != null) criadasSub.AddRange(s);
                });

            var controller = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
                mockEventoService.Object,
                Mock.Of<IMapper>(),
                mockInscricaoService.Object,
                mockPessoaService.Object,
                Mock.Of<ISubeventoService>());

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, UsernameTeste) };
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"))
            };
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "QuantidadeTipoInscricao_0", "0" }
            });
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var input = new InscricaoEventoModel
            {
                SelectedSubeventos = new List<uint>()
            };

            // Act
            var result = await controller.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("minhasInscricoes", redirect.ActionName);
            Assert.AreEqual("Você já está inscrito neste evento!", controller.TempData["ParticipanteMessage"]);
            Assert.AreEqual(0, criadasEvento.Count, "Não deve criar inscrição quando quantidade for 0 e nada for selecionado.");
            Assert.AreEqual(0, criadasSub.Count);
        }

        [TestMethod()]
        public async Task RealizarInscricaoTest_Post_SemDuplicataNoLote_MesmoSubeventoDuplicadoNoArray_InscreveApenasUmaVez()
        {
            // Arrange
            var tipos = new Dictionary<uint, Tipoinscricao>
            {
                { 1, new Tipoinscricao { Id = 1, IdEvento = 1, Nome = "Geral", Valor = 10m } },
                { 2, new Tipoinscricao { Id = 2, IdEvento = 1, Nome = "Sub Tipo", Valor = 20m } }
            };
            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.Get(10))
                .Returns(new Subevento
                {
                    Id = 10,
                    IdEvento = 1,
                    Nome = "Minicurso",
                    Status = "A",
                    DataFimInscricao = DateTime.Now.AddDays(1),
                    VagasDisponiveis = 10
                });

            var (postController, criadas, criadasSub) = CreatePostController(
                tipos,
                new Dictionary<string, string>
                {
                    { "QuantidadeTipoInscricao_1", "1" },
                    { "QuantidadeTipoInscricaoSubevento_10_2", "1" }
                },
                customSubeventoService: mockSubeventoService);

            var input = new InscricaoEventoModel
            {
                IdTipoInscricao = 1,
                SelectedSubeventos = new List<uint> { 10, 10 } // Array com duplicata
            };

            // Act
            var result = await postController.realizarInscricao(1, input);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual(1, criadas.Count);
            Assert.AreEqual(1, criadasSub.Count, "Subevento duplicado no lote de seleção deve ser processado apenas uma vez.");
        }

        private static InscricaoController CreateGetController(Evento evento)
        {
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Nome Participante", NomeCracha = "Apelido" });

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(evento.Id)).Returns(evento);

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(s => s.GetByEvento(evento.Id)).Returns(new List<Tipoinscricao>());

            var mockSubeventoService = new Mock<ISubeventoService>();
            mockSubeventoService.Setup(s => s.GetByIdEvento(evento.Id)).Returns(new List<SubeventoEventoDTO>());

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.GetGestorInEvent(UsernameTeste, evento.Id)).Returns((Inscricaopessoaevento)null!);
            mockInscricaoService.Setup(s => s.IsInscrito(1, evento.Id)).Returns(false);

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

            return getController;
        }

        private static (InscricaoController, List<Inscricaopessoaevento>, List<Inscricaopessoasubevento>) CreatePostController(
            Dictionary<uint, Tipoinscricao> tipos,
            Dictionary<string, string> formFields,
            Evento? evento = null,
            Pessoa? pessoa = null,
            Mock<ISubeventoService>? customSubeventoService = null)
        {
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(service => service.GetByCpf(UsernameTeste))
                .Returns(pessoa ?? new Pessoa { Id = 1, Cpf = UsernameTeste, Nome = "Participante Teste" });

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(service => service.IsInscrito(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns(false);
            var criadas = new List<Inscricaopessoaevento>();
            var criadasSub = new List<Inscricaopessoasubevento>();
            
            mockInscricaoService.Setup(service => service.CreateInscricoesEmLote(
                    It.IsAny<IEnumerable<Inscricaopessoaevento>>(), 
                    It.IsAny<IEnumerable<Inscricaopessoasubevento>>(),
                    It.IsAny<bool>()))
                .Callback<IEnumerable<Inscricaopessoaevento>, IEnumerable<Inscricaopessoasubevento>, bool>((e, s, _) => 
                {
                    if (e != null) criadas.AddRange(e);
                    if (s != null) criadasSub.AddRange(s);
                });

            var mockTipoService = new Mock<ITipoInscricaoService>();
            mockTipoService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns<uint>(id => tipos.TryGetValue(id, out var tipo) ? tipo : null);

            evento ??= new Evento { Id = 1, Status = "A", PossuiCertificado = 1, VagasDisponiveis = 100 };
            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(service => service.Get(It.IsAny<uint>()))
                .Returns(evento);

            var mockSubeventoService = customSubeventoService ?? new Mock<ISubeventoService>();
            if (customSubeventoService == null)
            {
                mockSubeventoService.Setup(service => service.Get(10))
                    .Returns(new Subevento { Id = 10, IdEvento = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1), ValorInscricao = 50m, VagasDisponiveis = 100 });
            }

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

        [TestMethod()]
        public void CancelarInscricaoTest_Dono_EventoAtivo_CancelaRestauraVagasERedireciona()
        {
            // Arrange
            var (cancelarController, mockInscricao, mockEvento, mockSubevento) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste, Nome = "Participante Teste" },
                evento: new Evento { Id = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(1) },
                cancelamento: (2, new List<uint> { 10 }));
            uint pessoaIdChamada = 0, eventoIdChamado = 0;
            mockInscricao.Setup(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()))
                .Callback<uint, uint>((p, e) => { pessoaIdChamada = p; eventoIdChamado = e; })
                .Returns((2, new List<uint> { 10 }));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert: usa o Id da pessoa logada (dono), nunca um id arbitrario
            Assert.AreEqual((uint)7, pessoaIdChamada);
            Assert.AreEqual((uint)1, eventoIdChamado);
            mockEvento.Verify(e => e.AtualizarVagasDisponiveis((uint)1, true), Times.Once);
            mockSubevento.Verify(s => s.AtualizarVagasDisponiveis((uint)10, true), Times.Once);
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
            Assert.AreEqual("Desistência realizada com sucesso! Sua vaga foi liberada.", cancelarController.TempData["ParticipanteSuccessMessage"]);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_EventoInativo_BloqueiaSemChamarService()
        {
            // Arrange
            var (cancelarController, mockInscricao, _, _) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste },
                evento: new Evento { Id = 1, Status = "F" },
                cancelamento: (0, new List<uint>()));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert
            mockInscricao.Verify(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()), Times.Never);
            Assert.AreEqual("Este evento não está ativo para desistência.", cancelarController.TempData["ParticipanteMessage"]);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_PrazoInscricaoEncerrado_PermiteDesistencia()
        {
            // Arrange: prazo de inscricao fechado NAO bloqueia (regra M1);
            // so o inicio/encerramento do evento bloqueia.
            var (cancelarController, mockInscricao, mockEvento, _) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste },
                evento: new Evento { Id = 1, Status = "A", DataFimInscricao = DateTime.Now.AddDays(-1), DataInicio = DateTime.Now.AddDays(5) },
                cancelamento: (1, new List<uint>()));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert
            mockInscricao.Verify(s => s.CancelarInscricaoParticipante((uint)7, (uint)1), Times.Once);
            mockEvento.Verify(e => e.AtualizarVagasDisponiveis((uint)1, true), Times.Once);
            Assert.AreEqual("Desistência realizada com sucesso! Sua vaga foi liberada.", cancelarController.TempData["ParticipanteSuccessMessage"]);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_EventoIniciado_BloqueiaSemChamarService()
        {
            // Arrange
            var (cancelarController, mockInscricao, _, _) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste },
                evento: new Evento { Id = 1, Status = "A", DataInicio = DateTime.Now.AddHours(-1) },
                cancelamento: (0, new List<uint>()));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert
            mockInscricao.Verify(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()), Times.Never);
            Assert.AreEqual("Este evento já foi iniciado e não permite mais desistência.", cancelarController.TempData["ParticipanteMessage"]);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_EventoEncerrado_BloqueiaSemChamarService()
        {
            // Arrange: sem DataInicio, mas com DataFim passada
            var (cancelarController, mockInscricao, _, _) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste },
                evento: new Evento { Id = 1, Status = "A", DataFim = DateTime.Now.AddDays(-1) },
                cancelamento: (0, new List<uint>()));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert
            mockInscricao.Verify(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()), Times.Never);
            Assert.AreEqual("Este evento já foi encerrado e não permite mais desistência.", cancelarController.TempData["ParticipanteMessage"]);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_FalhaNoService_RetornaMensagemAmigavel()
        {
            // Arrange: service lanca DbUpdateException (ex: duplo-clique concorrente)
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste))
                .Returns(new Pessoa { Id = 7, Cpf = UsernameTeste });
            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(It.IsAny<uint>()))
                .Returns(new Evento { Id = 1, Status = "A", DataInicio = DateTime.Now.AddDays(5) });
            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()))
                .Throws(new DbUpdateException("concorrencia simulada"));
            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();
            var ctrl = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
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
            ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
            ctrl.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            // Act
            var result = ctrl.CancelarInscricao(1);

            // Assert: sem 500, mensagem amigavel
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
            Assert.AreEqual("Não foi possível concluir a desistência. Tente novamente.", ctrl.TempData["ParticipanteMessage"]);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_Anonimo_RedirecionaHome()
        {
            // Arrange: identidade sem nome
            var (cancelarController, mockInscricao, _, _) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste },
                evento: new Evento { Id = 1, Status = "A" },
                cancelamento: (0, new List<uint>()));
            var anonContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            var original = cancelarController.ControllerContext;
            cancelarController.ControllerContext = new ControllerContext { HttpContext = anonContext };
            try
            {
                // Act
                var result = cancelarController.CancelarInscricao(1);

                // Assert
                mockInscricao.Verify(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()), Times.Never);
                Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
                var redirect = (RedirectToActionResult)result;
                Assert.AreEqual("Index", redirect.ActionName);
                Assert.AreEqual("Home", redirect.ControllerName);
            }
            finally
            {
                cancelarController.ControllerContext = original;
            }
        }

        [TestMethod()]
        public void CancelarInscricaoTest_SemInscricao_InformaNaoEncontradaSemAtualizarVagas()
        {
            // Arrange
            var (cancelarController, _, mockEvento, mockSubevento) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste },
                evento: new Evento { Id = 1, Status = "A" },
                cancelamento: (0, new List<uint>()));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert
            Assert.AreEqual("Nenhuma inscrição de participante encontrada para este evento.", cancelarController.TempData["ParticipanteMessage"]);
            mockEvento.Verify(e => e.AtualizarVagasDisponiveis(It.IsAny<uint>(), It.IsAny<bool>()), Times.Never);
            mockSubevento.Verify(s => s.AtualizarVagasDisponiveis(It.IsAny<uint>(), It.IsAny<bool>()), Times.Never);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_PessoaNaoEncontrada_RedirecionaSemChamarService()
        {
            // Arrange
            var (cancelarController, mockInscricao, _, _) = CreateCancelarController(
                pessoa: null,
                evento: new Evento { Id = 1, Status = "A" },
                cancelamento: (0, new List<uint>()));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert
            mockInscricao.Verify(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()), Times.Never);
            Assert.AreEqual("Usuário não encontrado.", cancelarController.TempData["ParticipanteMessage"]);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }

        [TestMethod()]
        public void CancelarInscricaoTest_EventoNaoEncontrado_RedirecionaSemChamarService()
        {
            // Arrange
            var (cancelarController, mockInscricao, _, _) = CreateCancelarController(
                pessoa: new Pessoa { Id = 7, Cpf = UsernameTeste },
                evento: null,
                cancelamento: (0, new List<uint>()));

            // Act
            var result = cancelarController.CancelarInscricao(1);

            // Assert
            mockInscricao.Verify(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()), Times.Never);
            Assert.AreEqual("Evento não encontrado.", cancelarController.TempData["ParticipanteMessage"]);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }

        private static (InscricaoController Controller, Mock<IInscricaoService> InscricaoService, Mock<IEventoService> EventoService, Mock<ISubeventoService> SubeventoService) CreateCancelarController(
            Pessoa? pessoa,
            Evento? evento,
            (int Removidas, List<uint> SubeventosAfetados) cancelamento)
        {
            var mockPessoaService = new Mock<IPessoaService>();
            mockPessoaService.Setup(s => s.GetByCpf(UsernameTeste)).Returns(pessoa!);

            var mockEventoService = new Mock<IEventoService>();
            mockEventoService.Setup(s => s.Get(It.IsAny<uint>())).Returns(evento!);

            var mockInscricaoService = new Mock<IInscricaoService>();
            mockInscricaoService.Setup(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns(cancelamento);

            var mockSubeventoService = new Mock<ISubeventoService>();

            IMapper mapper = new MapperConfiguration(cfg =>
                cfg.AddProfile(new InscricaoProfile())).CreateMapper();

            var cancelarController = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
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
            cancelarController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            cancelarController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            return (cancelarController, mockInscricaoService, mockEventoService, mockSubeventoService);
        }
    }
}
