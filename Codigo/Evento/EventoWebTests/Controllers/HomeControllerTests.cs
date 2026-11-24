using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Controllers;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace EventoWeb.Controllers.Tests
{
    [TestClass]
    public class HomeControllerTests
    {
        private Mock<ILogger<HomeController>> _mockLogger = null!;
        private Mock<IEventoService> _mockEventoService = null!;
        private Mock<ITipoeventoService> _mockTipoEventoService = null!;
        private Mock<IAreaInteresseService> _mockAreaInteresseService = null!;
        private Mock<IEstadosbrasilService> _mockEstadosBrasilService = null!;
        private Mock<IPessoaService> _mockPessoaService = null!;
        private IMapper _mapper = null!;

        [TestInitialize]
        public void Initialize()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _mockEventoService = new Mock<IEventoService>();
            _mockTipoEventoService = new Mock<ITipoeventoService>();
            _mockAreaInteresseService = new Mock<IAreaInteresseService>();
            _mockEstadosBrasilService = new Mock<IEstadosbrasilService>();
            _mockPessoaService = new Mock<IPessoaService>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new EventoProfile());
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private HomeController CreateController(HttpContext httpContext)
        {
            var controller = new HomeController(
                _mockLogger.Object,
                _mockEventoService.Object,
                _mapper,
                _mockTipoEventoService.Object,
                _mockAreaInteresseService.Object,
                _mockEstadosBrasilService.Object,
                _mockPessoaService.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
            return controller;
        }

        [TestMethod]
        public void Index_UsuarioAutenticadoSemCookie_NaoCarregaEstadoDoPerfil_DefineComoTodoOBrasil()
        {
            var userCpf = "12345678901";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userCpf),
                new Claim(ClaimTypes.Role, "PARTICIPANTE")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            _mockPessoaService.Setup(s => s.GetByCpf(userCpf))
                .Returns(new Pessoa { Cpf = userCpf, Estado = "SE" });

            _mockEventoService.Setup(s => s.GetEventosVitrine(null))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento Geral Brasil", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.Model, typeof(List<EventoModel>));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Todo o Brasil", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine(null), Times.Once);
        }

        [TestMethod]
        public void Index_ComCookieUserLocation_PrevaleceSobreCadastro()
        {
            var userCpf = "12345678901";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userCpf),
                new Claim(ClaimTypes.Role, "PARTICIPANTE")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            var cookieJson = "{\"cidade\":\"SÃ£o Paulo\",\"estado\":\"SP\",\"manual\":true}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockPessoaService.Setup(s => s.GetByCpf(userCpf))
                .Returns(new Pessoa { Cpf = userCpf, Estado = "SE" });

            _mockEstadosBrasilService.Setup(s => s.Get("SP"))
                .Returns(new Estadosbrasil { Estado = "SP", Nome = "SÃ£o Paulo" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("SP"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 2, Nome = "Evento SP", Estado = "SP", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SP", controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("SP"), Times.Once);
        }

        [TestMethod]
        public void Index_OpcaoTodoOBrasil_NaoFiltraPorEstado()
        {
            var httpContext = new DefaultHttpContext();
            var cookieJson = "{\"cidade\":\"\",\"estado\":\"\",\"manual\":true}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockEventoService.Setup(s => s.GetEventosVitrine(null))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento SE", Estado = "SE", Status = "A", DataFim = DateTime.Now.AddDays(5) },
                    new Evento { Id = 2, Nome = "Evento SP", Estado = "SP", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine(null), Times.Once);
        }

        [TestMethod]
        public void Index_OpcaoTodoOBrasilPorParametro_NaoFiltraPorEstado()
        {
            var httpContext = new DefaultHttpContext();
            _mockEventoService.Setup(s => s.GetEventosVitrine(null))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento Geral", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(uf: "TODOS", vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine(null), Times.Once);
        }

        [TestMethod]
        public void Index_VisitanteComCookieGeolocalizado_FiltraPorEstadoDoCookie()
        {
            var httpContext = new DefaultHttpContext();
            var cookieJson = "{\"cidade\":\"Rio de Janeiro\",\"estado\":\"RJ\",\"manual\":false}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockEstadosBrasilService.Setup(s => s.Get("RJ"))
                .Returns(new Estadosbrasil { Estado = "RJ", Nome = "Rio de Janeiro" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("RJ"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 4, Nome = "Evento RJ", Estado = "RJ", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("RJ", controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("RJ"), Times.Once);
        }

        [TestMethod]
        public void Index_UsuarioAutenticadoComCookie_PrevaleceLocalizacaoDoCookie()
        {
            var userCpf = "12345678901";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userCpf),
                new Claim(ClaimTypes.Role, "PARTICIPANTE")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            var cookieJson = "{\"cidade\":\"SÃ£o Paulo\",\"estado\":\"SP\",\"manual\":false}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockPessoaService.Setup(s => s.GetByCpf(userCpf))
                .Returns(new Pessoa { Cpf = userCpf, Estado = "SE" });

            _mockEstadosBrasilService.Setup(s => s.Get("SP"))
                .Returns(new Estadosbrasil { Estado = "SP", Nome = "SÃ£o Paulo" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("SP"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento SP", Estado = "SP", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SP", controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("SÃ£o Paulo (SP)", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("SP"), Times.Once);
        }

        [TestMethod]
        public void Index_ComUfInvalida_DescartaETrataComoTodoOBrasil()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("ZZ"))
                .Returns((Estadosbrasil)null!);

            _mockEventoService.Setup(s => s.GetEventosVitrine(null))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento Brasil", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(uf: "ZZ", vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Todo o Brasil", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine(null), Times.Once);
        }

        [TestMethod]
        public void Buscar_SemEstadoNoFiltro_NaoUsaFallbackDeCookieOuPessoa_DefineComoTodoOBrasil()
        {
            var userCpf = "12345678901";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userCpf),
                new Claim(ClaimTypes.Role, "PARTICIPANTE")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            var cookieJson = "{\"cidade\":\"SÃ£o Paulo\",\"estado\":\"SP\",\"manual\":true}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockPessoaService.Setup(s => s.GetByCpf(userCpf))
                .Returns(new Pessoa { Cpf = userCpf, Estado = "SE" });

            int totalRegistros = 0;
            _mockEventoService.Setup(s => s.Search(It.IsAny<Core.DTO.EventoFilterDTO>(), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento>());
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>());
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { TermoBusca = "Tecnologia" };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Todo o Brasil", controller.ViewBag.NomeEstadoAtivo);
        }

        [TestMethod]
        public void Buscar_ComEstadoNoFiltroValido_AlinhaHeaderAoEstadoDoFiltro()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("SP"))
                .Returns(new Estadosbrasil { Estado = "SP", Nome = "SÃ£o Paulo" });

            int totalRegistros = 0;
            _mockEventoService.Setup(s => s.Search(It.IsAny<Core.DTO.EventoFilterDTO>(), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento>());
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>());
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { Estado = "sp" };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SP", controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("SÃ£o Paulo (SP)", controller.ViewBag.NomeEstadoAtivo);
        }

        [TestMethod]
        public void Buscar_ComEstadoTodosOuInvalido_DefineComoTodoOBrasil()
        {
            var httpContext = new DefaultHttpContext();
            int totalRegistros = 0;
            _mockEventoService.Setup(s => s.Search(It.IsAny<Core.DTO.EventoFilterDTO>(), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento>());
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>());
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { Estado = "TODOS" };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Todo o Brasil", controller.ViewBag.NomeEstadoAtivo);
        }
        [TestMethod]
        public void Buscar_ComIdTipoEvento_PassaFiltroParaServiceERetornaModel()
        {
            var httpContext = new DefaultHttpContext();
            int totalRegistros = 1;
            var eventoShow = new Evento { Id = 10, Nome = "Festival de Música", IdTipoEvento = 6, Status = "A" };

            _mockEventoService.Setup(s => s.Search(It.Is<Core.DTO.EventoFilterDTO>(f => f.IdTipoEvento == 6), 1, 16, out totalRegistros))
                .Returns(new List<Evento> { eventoShow });
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento> { new Tipoevento { Id = 6, Nome = "Show" } });
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { IdTipoEvento = 6 };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.Model, typeof(List<EventoModel>));
            var model = (List<EventoModel>)viewResult.Model!;
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("Festival de Música", model[0].Nome);
            _mockEventoService.Verify(s => s.Search(It.Is<Core.DTO.EventoFilterDTO>(f => f.IdTipoEvento == 6), 1, 16, out It.Ref<int>.IsAny), Times.Once);
        }

        [TestMethod]
        public void Buscar_ComIdTipoEventoEEstado_AplicaAmbosFiltrosEAlinhaEstadoAtivo()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            int totalRegistros = 1;
            var eventoPalestra = new Evento { Id = 20, Nome = "Palestra de TI", IdTipoEvento = 1, Estado = "SE", Status = "A" };

            _mockEventoService.Setup(s => s.Search(It.Is<Core.DTO.EventoFilterDTO>(f => f.IdTipoEvento == 1 && f.Estado == "SE"), 1, 16, out totalRegistros))
                .Returns(new List<Evento> { eventoPalestra });
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento> { new Tipoevento { Id = 1, Nome = "Palestra" } });
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { IdTipoEvento = 1, Estado = "SE" };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.Model, typeof(List<EventoModel>));
            var model = (List<EventoModel>)viewResult.Model!;
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Sergipe (SE)", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.Search(It.Is<Core.DTO.EventoFilterDTO>(f => f.IdTipoEvento == 1 && f.Estado == "SE"), 1, 16, out It.Ref<int>.IsAny), Times.Once);
        }

        [TestMethod]
        public void Index_ComCookieManualContendoCidade_DefineCidadeAtivaENomeExibicaoComCidadeEEstado()
        {
            var httpContext = new DefaultHttpContext();
            var cookieJson = "{\"cidade\":\"Itabaiana\",\"estado\":\"SE\",\"manual\":true}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("SE"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 10, Nome = "Evento Itabaiana", Estado = "SE", Status = "A", DataFim = DateTime.Now.AddDays(3) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Itabaiana", controller.ViewBag.CidadeAtiva);
            Assert.AreEqual("Itabaiana (SE)", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("SE"), Times.Once);
        }

        [TestMethod]
        public void Index_ComCookieManualSemCidade_DefineNomeExibicaoComEstadoEEstado()
        {
            var httpContext = new DefaultHttpContext();
            var cookieJson = "{\"cidade\":\"\",\"estado\":\"SE\",\"manual\":true}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("SE"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 11, Nome = "Evento Geral SE", Estado = "SE", Status = "A", DataFim = DateTime.Now.AddDays(3) }
                });

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
            Assert.IsNull(controller.ViewBag.CidadeAtiva);
            Assert.AreEqual("Sergipe (SE)", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("SE"), Times.Once);
        }

        [TestMethod]
        public void Buscar_ComEstadoECidadeNoFiltro_DefineCidadeAtivaENomeExibicaoComCidadeEEstado()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            int totalRegistros = 0;
            _mockEventoService.Setup(s => s.Search(It.IsAny<Core.DTO.EventoFilterDTO>(), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento>());
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>());
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { Estado = "SE", Cidade = "Itabaiana" };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Itabaiana", controller.ViewBag.CidadeAtiva);
            Assert.AreEqual("Itabaiana (SE)", controller.ViewBag.NomeEstadoAtivo);
        }

        [TestMethod]
        public void Buscar_ComEstadoNoFiltroESemCidade_DefineNomeExibicaoComEstadoEEstado()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            int totalRegistros = 0;
            _mockEventoService.Setup(s => s.Search(It.IsAny<Core.DTO.EventoFilterDTO>(), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento>());
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>());
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { Estado = "SE", Cidade = "" };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
            Assert.IsNull(controller.ViewBag.CidadeAtiva);
            Assert.AreEqual("Sergipe (SE)", controller.ViewBag.NomeEstadoAtivo);
        }

        [TestMethod]
        public void Index_DeveDefinirCategoriasHomeComMapeamentoResilienteEDinamico()
        {
            var httpContext = new DefaultHttpContext();
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>
            {
                new Tipoevento { Id = 1, Nome = "Palestra" },
                new Tipoevento { Id = 6, Nome = "Show" }
            });
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>
            {
                new Areainteresse { Id = 4, Nome = "Exposição Cultural" }
            });
            _mockEventoService.Setup(s => s.GetEventosVitrine(null)).Returns(new List<Evento>());

            var controller = CreateController(httpContext);

            var result = controller.Index(vitrine: true);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var categorias = controller.ViewBag.CategoriasHome as Dictionary<string, CategoriaHomeRouteModel>;
            Assert.IsNotNull(categorias);
            Assert.IsTrue(categorias.ContainsKey("Palestras"));
            Assert.AreEqual((uint)1, categorias["Palestras"].IdTipoEvento);

            Assert.IsTrue(categorias.ContainsKey("Shows"));
            Assert.AreEqual((uint)6, categorias["Shows"].IdTipoEvento);

            Assert.IsTrue(categorias.ContainsKey("Culturais"));
            Assert.AreEqual((uint)4, categorias["Culturais"].IdAreaInteresse);

            Assert.IsTrue(categorias.ContainsKey("Teatros"));
            Assert.AreEqual("Teatro", categorias["Teatros"].TermoBusca);

            Assert.IsTrue(categorias.ContainsKey("Stand-Up"));
            Assert.AreEqual("Stand-Up", categorias["Stand-Up"].TermoBusca);

            Assert.IsTrue(categorias.ContainsKey("Religiosos"));
            Assert.AreEqual("Religioso", categorias["Religiosos"].TermoBusca);
        }

        [TestMethod]
        public void Buscar_ComFiltroCategoriaPalestrasEEstado_AplicaFiltroETipoEventoEEstado()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            int totalRegistros = 1;
            var eventoEsperado = new Evento { Id = 10, Nome = "Palestra de IA", IdTipoEvento = 1, Estado = "SE", Status = "A" };
            _mockEventoService.Setup(s => s.Search(It.Is<Core.DTO.EventoFilterDTO>(f => f.IdTipoEvento == 1 && f.Estado == "SE"), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento> { eventoEsperado });
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento> { new Tipoevento { Id = 1, Nome = "Palestra" } });
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { IdTipoEvento = 1, Estado = "SE" };

            var result = controller.Buscar(filter);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as List<EventoModel>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual((uint)10, model[0].Id);
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
        }

        [TestMethod]
        public void Buscar_ComParametroCategoriaShowsEEstado_MapeiaDinamicamenteERespeitaEstado()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>
            {
                new Tipoevento { Id = 6, Nome = "Show" }
            });
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            int totalRegistros = 1;
            var eventoShow = new Evento { Id = 20, Nome = "Show de Verão", IdTipoEvento = 6, Estado = "SE", Status = "A" };
            _mockEventoService.Setup(s => s.Search(It.Is<Core.DTO.EventoFilterDTO>(f => f.IdTipoEvento == 6 && f.Estado == "SE"), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento> { eventoShow });

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { Estado = "SE" };

            var result = controller.Buscar(filter, categoria: "Shows");

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as List<EventoModel>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual((uint)20, model[0].Id);
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
        }

        [TestMethod]
        public void Buscar_ComParametroCategoriaTeatrosEEstado_AplicaTermoBuscaTeatroEEstado()
        {
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("BA"))
                .Returns(new Estadosbrasil { Estado = "BA", Nome = "Bahia" });
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>());
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            int totalRegistros = 1;
            var eventoTeatro = new Evento { Id = 30, Nome = "Peça de Teatro Musical", Estado = "BA", Status = "A" };
            _mockEventoService.Setup(s => s.Search(It.Is<Core.DTO.EventoFilterDTO>(f => f.TermoBusca == "Teatro" && f.Estado == "BA"), It.IsAny<int>(), It.IsAny<int>(), out totalRegistros))
                .Returns(new List<Evento> { eventoTeatro });

            var controller = CreateController(httpContext);
            var filter = new Core.DTO.EventoFilterDTO { Estado = "BA" };

            var result = controller.Buscar(filter, categoria: "Teatros");

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as List<EventoModel>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual((uint)30, model[0].Id);
            Assert.AreEqual("BA", controller.ViewBag.EstadoAtivo);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public void Buscar_ComPaginaZeroOuNegativa_AjustaParaPaginaUm(int paginaInvalida)
        {
            var httpContext = new DefaultHttpContext();
            int totalRegistros = 1;
            var evento = new Evento { Id = 40, Nome = "Evento Pagina 1", Status = "A" };
            _mockEventoService
                .Setup(s => s.Search(It.IsAny<Core.DTO.EventoFilterDTO>(), 1, 16, out totalRegistros))
                .Returns(new List<Evento> { evento });
            _mockTipoEventoService.Setup(s => s.GetAll()).Returns(new List<Tipoevento>());
            _mockAreaInteresseService.Setup(s => s.GetAll()).Returns(new List<Areainteresse>());
            _mockEstadosBrasilService.Setup(s => s.GetAll()).Returns(new List<Estadosbrasil>());

            var controller = CreateController(httpContext);
            var result = controller.Buscar(new Core.DTO.EventoFilterDTO(), pagina: paginaInvalida);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual(1, controller.ViewBag.PaginaAtual);
            _mockEventoService.Verify(s => s.Search(It.IsAny<Core.DTO.EventoFilterDTO>(), 1, 16, out It.Ref<int>.IsAny), Times.Once);
        }
    }
}