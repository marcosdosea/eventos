using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace EventoWeb.Controllers.Tests
{
    [TestClass]
    public class ModelocertificadoControllerTests
    {
        private const string CpfGestor = "12345678900";
        private const uint EventoProprio = 1;
        private const uint EventoAlheio = 2;

        private static IMapper CriarMapperReal() =>
            new MapperConfiguration(c => c.AddProfile<ModelocertificadoProfile>()).CreateMapper();

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
                new Modelocertificado { Id = 11, IdEvento = EventoProprio, IdEventoNavigation = new Evento { Id = EventoProprio, Nome = "Evento 1" } },
                new Modelocertificado { Id = 10, IdEvento = EventoAlheio, IdEventoNavigation = new Evento { Id = EventoAlheio, Nome = "Evento 2" } }
            });
            mockSvc.Setup(s => s.Get(10)).Returns(new Modelocertificado { Id = 10, IdEvento = EventoAlheio });
            mockSvc.Setup(s => s.Get(11)).Returns(new Modelocertificado { Id = 11, IdEvento = EventoProprio });
            var mockEvento = new Mock<IEventoService>();
            mockEvento.Setup(s => s.GetAll()).Returns(new List<Evento>());
            mockEvento.Setup(s => s.GetEventByCpf(It.IsAny<string>(), It.IsAny<uint>())).Returns(new List<Evento>());
            var ctl = new ModelocertificadoController(mockSvc.Object, mockEvento.Object, MockInscricaoSozinhoEventoProprio().Object, CriarMapperReal());
            SetUser(ctl, GestorPrincipal());

            Assert.IsInstanceOfType(ctl.Index(EventoAlheio), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Index(EventoProprio), typeof(ViewResult));
            Assert.IsInstanceOfType(ctl.Details(10), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Details(11), typeof(ViewResult));
            Assert.IsInstanceOfType(ctl.Create(EventoAlheio), typeof(ForbidResult));
            Assert.IsInstanceOfType(ctl.Create(new ModelocertificadoModel { IdEvento = EventoAlheio }), typeof(ForbidResult));
            var editAlheio = ctl.Edit(10, new ModelocertificadoModel { Id = 10, IdEvento = EventoAlheio });
            Assert.IsInstanceOfType(editAlheio, typeof(ForbidResult));
            // id da rota divergente do corpo -> 400
            Assert.IsInstanceOfType(ctl.Edit(11, new ModelocertificadoModel { Id = 99, IdEvento = EventoProprio }), typeof(BadRequestResult));
        }

        [TestMethod]
        public void Create_Get_ComIdEventoAutorizado_PreSelecionaEventoEDefineViewData()
        {
            var mockSvc = new Mock<IModelocertificadoService>();
            var mockEvento = new Mock<IEventoService>();
            mockEvento.Setup(s => s.GetEventByCpf(CpfGestor, 2))
                .Returns(new List<Evento> { new Evento { Id = EventoProprio, Nome = "SEMINFO" } });

            var ctl = new ModelocertificadoController(mockSvc.Object, mockEvento.Object, MockInscricaoSozinhoEventoProprio().Object, CriarMapperReal());
            SetUser(ctl, GestorPrincipal());

            var result = ctl.Create(EventoProprio) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as ModelocertificadoModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(EventoProprio, model.IdEvento);
            Assert.AreEqual(EventoProprio, ctl.ViewData["EventoId"]);
        }

        [TestMethod]
        public void Create_Post_Valido_MapeiaComProfileERedirecionaComIdEvento()
        {
            var mockSvc = new Mock<IModelocertificadoService>();
            Modelocertificado? entidadeCriada = null;
            mockSvc.Setup(s => s.Create(It.IsAny<Modelocertificado>()))
                .Callback<Modelocertificado>(m => entidadeCriada = m)
                .Returns(1u);
            var mockEvento = new Mock<IEventoService>();

            var ctl = new ModelocertificadoController(mockSvc.Object, mockEvento.Object, MockInscricaoSozinhoEventoProprio().Object, CriarMapperReal());
            SetUser(ctl, GestorPrincipal());

            var dataEmissao = new DateTime(2026, 11, 24);
            var result = ctl.Create(new ModelocertificadoModel
            {
                IdEvento = EventoProprio,
                DataEmissao = dataEmissao,
                Codigo = "CERT-2026-01"
            }) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(EventoProprio, result.RouteValues?["idEvento"]);
            Assert.IsNotNull(entidadeCriada);
            Assert.AreEqual(EventoProprio, entidadeCriada.IdEvento);
            Assert.AreEqual("CERT-2026-01", entidadeCriada.TextoAntesParticipante);
            Assert.AreEqual("24/11/2026", entidadeCriada.Assinatura1Texto);
            Assert.IsNotNull(entidadeCriada.LogotipoSuperior);
            Assert.IsNotNull(entidadeCriada.Assinatura1);
            Assert.IsFalse(string.IsNullOrWhiteSpace(entidadeCriada.TextoAntesEvento));
            Assert.IsFalse(string.IsNullOrWhiteSpace(entidadeCriada.TextoAntesCargaHoraria));
        }

        [TestMethod]
        public void Edit_Post_Valido_MapeiaComProfileERedirecionaComIdEvento()
        {
            var mockSvc = new Mock<IModelocertificadoService>();
            var logoExistente = new byte[] { 1, 2, 3 };
            mockSvc.Setup(s => s.Get(11)).Returns(new Modelocertificado
            {
                Id = 11,
                IdEvento = EventoProprio,
                TextoAntesParticipante = "OLD",
                LogotipoSuperior = logoExistente,
                Assinatura1 = new byte[] { 4, 5 }
            });
            Modelocertificado? entidadeAtualizada = null;
            mockSvc.Setup(s => s.Update(It.IsAny<Modelocertificado>()))
                .Callback<Modelocertificado>(m => entidadeAtualizada = m);

            var ctl = new ModelocertificadoController(mockSvc.Object, new Mock<IEventoService>().Object, MockInscricaoSozinhoEventoProprio().Object, CriarMapperReal());
            SetUser(ctl, GestorPrincipal());

            var result = ctl.Edit(11, new ModelocertificadoModel
            {
                Id = 11,
                IdEvento = EventoProprio,
                DataEmissao = new DateTime(2026, 12, 1),
                Codigo = "NEW-2026"
            }) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(EventoProprio, result.RouteValues?["idEvento"]);
            Assert.IsNotNull(entidadeAtualizada);
            Assert.AreEqual("NEW-2026", entidadeAtualizada.TextoAntesParticipante);
            Assert.AreEqual("01/12/2026", entidadeAtualizada.Assinatura1Texto);
            CollectionAssert.AreEqual(logoExistente, entidadeAtualizada.LogotipoSuperior);
        }

        [TestMethod]
        public void DeleteConfirmed_Valido_RedirecionaComIdEvento()
        {
            var mockSvc = new Mock<IModelocertificadoService>();
            mockSvc.Setup(s => s.Get(11)).Returns(new Modelocertificado { Id = 11, IdEvento = EventoProprio });

            var ctl = new ModelocertificadoController(mockSvc.Object, new Mock<IEventoService>().Object, MockInscricaoSozinhoEventoProprio().Object, CriarMapperReal());
            SetUser(ctl, GestorPrincipal());

            var result = ctl.DeleteConfirmed(11) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(EventoProprio, result.RouteValues?["idEvento"]);
            mockSvc.Verify(s => s.Delete(11), Times.Once);
        }
    }
}
