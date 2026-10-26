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
        public void Index_UsuarioAutenticadoComUFCadastrada_FiltraPorEstadoDoUsuario()
        {
            // Arrange
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

            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("SE"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento Sergipe", Estado = "SE", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            // Act
            var result = controller.Index(vitrine: true);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.Model, typeof(List<EventoModel>));
            var model = (List<EventoModel>)viewResult.Model!;
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("SE"), Times.Once);
        }

        [TestMethod]
        public void Index_ComCookieUserLocation_PrevaleceSobreCadastro()
        {
            // Arrange
            var userCpf = "12345678901";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userCpf),
                new Claim(ClaimTypes.Role, "PARTICIPANTE")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            // Cookie com seleção manual em SP
            var cookieJson = "{\"cidade\":\"São Paulo\",\"estado\":\"SP\",\"manual\":true}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockPessoaService.Setup(s => s.GetByCpf(userCpf))
                .Returns(new Pessoa { Cpf = userCpf, Estado = "SE" });

            _mockEstadosBrasilService.Setup(s => s.Get("SP"))
                .Returns(new Estadosbrasil { Estado = "SP", Nome = "São Paulo" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("SP"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 2, Nome = "Evento SP", Estado = "SP", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            // Act
            var result = controller.Index(vitrine: true);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SP", controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("SP"), Times.Once);
        }

        [TestMethod]
        public void Index_OpcaoTodoOBrasil_NaoFiltraPorEstado()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            // Cookie com seleção manual vazia ("Todo o Brasil")
            var cookieJson = "{\"cidade\":\"\",\"estado\":\"\",\"manual\":true}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockEventoService.Setup(s => s.GetEventosVitrine(null))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento SE", Estado = "SE", Status = "A", DataFim = DateTime.Now.AddDays(5) },
                    new Evento { Id = 2, Nome = "Evento SP", Estado = "SP", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            // Act
            var result = controller.Index(vitrine: true);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine(null), Times.Once);
        }

        [TestMethod]
        public void Index_OpcaoTodoOBrasilPorParametro_NaoFiltraPorEstado()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _mockEventoService.Setup(s => s.GetEventosVitrine(null))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento Geral", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            // Act
            var result = controller.Index(uf: "TODOS", vitrine: true);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine(null), Times.Once);
        }

        [TestMethod]
        public void Index_VisitanteComCookieGeolocalizado_FiltraPorEstadoDoCookie()
        {
            // Arrange - Visitante não autenticado
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

            // Act
            var result = controller.Index(vitrine: true);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("RJ", controller.ViewBag.EstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("RJ"), Times.Once);
        }

        [TestMethod]
        public void Index_UsuarioAutenticadoComCookieNaoManual_PrevaleceEstadoDoCadastro()
        {
            // Arrange - Usuário de SE autenticado, mas com cookie detectado automaticamente em SP
            var userCpf = "12345678901";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userCpf),
                new Claim(ClaimTypes.Role, "PARTICIPANTE")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            var cookieJson = "{\"cidade\":\"São Paulo\",\"estado\":\"SP\",\"manual\":false}";
            httpContext.Request.Headers.Append("Cookie", $"UserLocation={Uri.EscapeDataString(cookieJson)}");

            _mockPessoaService.Setup(s => s.GetByCpf(userCpf))
                .Returns(new Pessoa { Cpf = userCpf, Estado = "SE" });

            _mockEstadosBrasilService.Setup(s => s.Get("SE"))
                .Returns(new Estadosbrasil { Estado = "SE", Nome = "Sergipe" });

            _mockEventoService.Setup(s => s.GetEventosVitrine("SE"))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento SE", Estado = "SE", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            // Act
            var result = controller.Index(vitrine: true);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("SE", controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Sergipe (SE)", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine("SE"), Times.Once);
        }

        [TestMethod]
        public void Index_ComUfInvalida_DescartaETrataComoTodoOBrasil()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _mockEstadosBrasilService.Setup(s => s.Get("ZZ"))
                .Returns((Estadosbrasil)null!);

            _mockEventoService.Setup(s => s.GetEventosVitrine(null))
                .Returns(new List<Evento>
                {
                    new Evento { Id = 1, Nome = "Evento Brasil", Status = "A", DataFim = DateTime.Now.AddDays(5) }
                });

            var controller = CreateController(httpContext);

            // Act
            var result = controller.Index(uf: "ZZ", vitrine: true);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(controller.ViewBag.EstadoAtivo);
            Assert.AreEqual("Todo o Brasil", controller.ViewBag.NomeEstadoAtivo);
            _mockEventoService.Verify(s => s.GetEventosVitrine(null), Times.Once);
        }
    }
}
