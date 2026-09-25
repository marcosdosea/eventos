using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace EventoWeb.Controllers.Tests
{
    /// <summary>
    /// IDOR: gestor só pode gerenciar o próprio evento.
    /// Evento próprio (1) -> 200 (View/Redirect). Evento alheio (2) -> 403 (Forbid) ou 404.
    /// </summary>
    [TestClass]
    public class AutorizacaoEventoAlheioTests
    {
        private const string CpfGestor = "12345678900";
        private const uint EventoProprio = 1;
        private const uint EventoAlheio = 2;

        private static ClaimsPrincipal GestorPrincipal() => new(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.Name, CpfGestor),
            new Claim(ClaimTypes.Role, "GESTOR")
        }, "TestAuthType"));

        private static ClaimsPrincipal AdminPrincipal() => new(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "ADMINISTRADOR")
        }, "TestAuthType")).Identities);

        private static void SetUser(Controller c, ClaimsPrincipal user)
        {
            c.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private static Mock<IInscricaoService> MockInscricaoSozinhoEventoProprio()
        {
            var mock = new Mock<IInscricaoService>();
            mock.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), EventoProprio))
                .Returns(new Inscricaopessoaevento { IdPessoa = 1, IdEvento = EventoProprio, IdPapel = 2 });
            mock.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), EventoAlheio))
                .Returns((Inscricaopessoaevento)null!);
            return mock;
        }

        [TestMethod]
        public void TipoInscricao_Index_EventoAlheio_Forbid_Proprio_Ok()
        {
            var mockTipo = new Mock<ITipoInscricaoService>();
            mockTipo.Setup(s => s.GetByEvento(It.IsAny<uint>())).Returns(new List<Tipoinscricao>());
            var mockEvento = new Mock<IEventoService>();
            mockEvento.Setup(s => s.GetNomeById(It.IsAny<uint>())).Returns("Ev");
            var mapper = new MapperConfiguration(c => c.AddProfile(new TipoInscricaoProfile())).CreateMapper();
            var ctl = new TipoInscricaoController(mockTipo.Object, mapper, mockEvento.Object, new Mock<ISubeventoService>().Object, MockInscricaoSozinhoEventoProprio().Object);
            SetUser(ctl, GestorPrincipal());

            Assert.IsInstanceOfType(ctl.Index(EventoAlheio), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Index(EventoProprio), typeof(ViewResult));
        }

        [TestMethod]
        public void TipoInscricao_Details_EventoAlheio_Forbid()
        {
            var mockTipo = new Mock<ITipoInscricaoService>();
            mockTipo.Setup(s => s.Get(10)).Returns(new Tipoinscricao { Id = 10, IdEvento = EventoAlheio, Nome = "X" });
            mockTipo.Setup(s => s.Get(11)).Returns(new Tipoinscricao { Id = 11, IdEvento = EventoProprio, Nome = "Y" });
            var mapper = new MapperConfiguration(c => c.AddProfile(new TipoInscricaoProfile())).CreateMapper();
            var ctl = new TipoInscricaoController(mockTipo.Object, mapper, new Mock<IEventoService>().Object, new Mock<ISubeventoService>().Object, MockInscricaoSozinhoEventoProprio().Object);
            SetUser(ctl, GestorPrincipal());

            Assert.IsInstanceOfType(ctl.Details(10), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Details(11), typeof(ViewResult));
        }

        [TestMethod]
        public void Subevento_Details_EventoAlheio_Forbid_Proprio_Ok()
        {
            var mockSub = new Mock<ISubeventoService>();
            mockSub.Setup(s => s.Get(10)).Returns(new Subevento { Id = 10, IdEvento = EventoAlheio, Nome = "A" });
            mockSub.Setup(s => s.Get(11)).Returns(new Subevento { Id = 11, IdEvento = EventoProprio, Nome = "B" });
            var mapper = new MapperConfiguration(c => c.AddProfile(new SubeventoProfile())).CreateMapper();
            var ctl = new SubeventoController(mockSub.Object, mapper, new Mock<IEventoService>().Object, new Mock<ITipoeventoService>().Object, new Mock<ITipoInscricaoService>().Object, MockInscricaoSozinhoEventoProprio().Object);
            SetUser(ctl, GestorPrincipal());

            Assert.IsInstanceOfType(ctl.Details(10), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Details(11), typeof(ViewResult));
        }

        [TestMethod]
        public void Subevento_CreateOrEdit_EventoAlheio_Forbid()
        {
            var mapper = new MapperConfiguration(c => c.AddProfile(new SubeventoProfile())).CreateMapper();
            var ctl = new SubeventoController(new Mock<ISubeventoService>().Object, mapper, new Mock<IEventoService>().Object, new Mock<ITipoeventoService>().Object, new Mock<ITipoInscricaoService>().Object, MockInscricaoSozinhoEventoProprio().Object);
            SetUser(ctl, GestorPrincipal());

            Assert.IsInstanceOfType(ctl.CreateOrEdit(EventoAlheio, (uint?)null), typeof(ForbidResult));
        }

        [TestMethod]
        public void Modelocracha_Index_Details_EventoAlheio_Forbid()
        {
            var mockCracha = new Mock<IModelocrachaService>();
            mockCracha.Setup(s => s.GetByEvento(It.IsAny<uint>())).Returns(new List<Modelocracha>());
            mockCracha.Setup(s => s.Get(10)).Returns(new Modelocracha { Id = 10, IdEvento = EventoAlheio });
            mockCracha.Setup(s => s.Get(11)).Returns(new Modelocracha { Id = 11, IdEvento = EventoProprio });
            var mapper = new MapperConfiguration(c => c.AddProfile(new ModeloCrachaProfile())).CreateMapper();
            var ctl = new ModelocrachaController(mockCracha.Object, new Mock<IEventoService>().Object, new Mock<IPessoaService>().Object, MockInscricaoSozinhoEventoProprio().Object, mapper);
            SetUser(ctl, GestorPrincipal());

            Assert.IsInstanceOfType(ctl.Index(EventoAlheio, null), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Index(EventoProprio, null), typeof(ViewResult));
            Assert.IsInstanceOfType(ctl.Details(10, null), typeof(ForbidResult));
        }

        [TestMethod]
        public void Modelocertificado_Index_Details_Create_Edit_EventoAlheio_Forbid()
        {
            var mockSvc = new Mock<IModelocertificadoService>();
            mockSvc.Setup(s => s.GetAll()).Returns(new List<Modelocertificado>
            {
                new Modelocertificado { Id = 11, IdEvento = EventoProprio },
                new Modelocertificado { Id = 10, IdEvento = EventoAlheio }
            });
            mockSvc.Setup(s => s.Get(10)).Returns(new Modelocertificado { Id = 10, IdEvento = EventoAlheio });
            mockSvc.Setup(s => s.Get(11)).Returns(new Modelocertificado { Id = 11, IdEvento = EventoProprio });
            var mockEvento = new Mock<IEventoService>();
            mockEvento.Setup(s => s.GetAll()).Returns(new List<Evento>());
            mockEvento.Setup(s => s.GetEventByCpf(It.IsAny<string>(), It.IsAny<uint>())).Returns(new List<Evento>());
            var mapper = new MapperConfiguration(c =>
            {
                c.CreateMap<Modelocertificado, ModelocertificadoModel>();
                c.CreateMap<ModelocertificadoModel, Modelocertificado>();
            }).CreateMapper();
            var ctl = new ModelocertificadoController(mockSvc.Object, mockEvento.Object, MockInscricaoSozinhoEventoProprio().Object, mapper);
            SetUser(ctl, GestorPrincipal());

            Assert.IsInstanceOfType(ctl.Index(EventoAlheio), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Index(EventoProprio), typeof(ViewResult));
            Assert.IsInstanceOfType(ctl.Details(10), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Details(11), typeof(ViewResult));
            Assert.IsInstanceOfType(ctl.Create(new ModelocertificadoModel { IdEvento = EventoAlheio }), typeof(ForbidResult));
            var editAlheio = ctl.Edit(10, new ModelocertificadoModel { Id = 10, IdEvento = EventoAlheio });
            Assert.IsInstanceOfType(editAlheio, typeof(ForbidResult));
            // id da rota divergente do corpo -> 400
            Assert.IsInstanceOfType(ctl.Edit(11, new ModelocertificadoModel { Id = 99, IdEvento = EventoProprio }), typeof(BadRequestResult));
        }

        [TestMethod]
        public void Administrador_Libera_EventoAlheio()
        {
            var mockTipo = new Mock<ITipoInscricaoService>();
            mockTipo.Setup(s => s.GetByEvento(It.IsAny<uint>())).Returns(new List<Tipoinscricao>());
            var mockEvento = new Mock<IEventoService>();
            mockEvento.Setup(s => s.GetNomeById(It.IsAny<uint>())).Returns("Ev");
            var mockInsc = new Mock<IInscricaoService>();
            mockInsc.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns((Inscricaopessoaevento)null!);
            var mapper = new MapperConfiguration(c => c.AddProfile(new TipoInscricaoProfile())).CreateMapper();
            var ctl = new TipoInscricaoController(mockTipo.Object, mapper, mockEvento.Object, new Mock<ISubeventoService>().Object, mockInsc.Object);
            SetUser(ctl, AdminPrincipal());

            Assert.IsInstanceOfType(ctl.Index(EventoAlheio), typeof(ViewResult));
        }
    }
}
