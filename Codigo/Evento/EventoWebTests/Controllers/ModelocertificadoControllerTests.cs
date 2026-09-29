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
    [TestClass]
    public class ModelocertificadoControllerTests
    {
        private const string CpfGestor = "12345678900";
        private const uint EventoProprio = 1;
        private const uint EventoAlheio = 2;

        private static ClaimsPrincipal GestorPrincipal() => new(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.Name, CpfGestor),
            new Claim(ClaimTypes.Role, "GESTOR")
        }, "TestAuthType"));

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

        // IDOR (#764): gestor só gerencia o próprio evento.
        [TestMethod]
        public void Index_Details_Create_Edit_EventoAlheio_Forbid()
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
    }
}
