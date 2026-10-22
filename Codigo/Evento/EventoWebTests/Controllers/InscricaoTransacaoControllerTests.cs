using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace EventoWeb.Controllers.Tests
{
    /// <summary>
    /// Garante que os casos de uso críticos rodam dentro de uma transação
    /// que cobre a operação inteira (Issue #841).
    /// </summary>
    [TestClass()]
    public class InscricaoTransacaoControllerTests
    {
        private const string UsuarioTeste = "11111111111";

        private static ClaimsPrincipal Principal()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, UsuarioTeste) };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
        }

        private static Mock<ITransacaoService> CriarTransacaoMock(Action? aoExecutar = null)
        {
            var mock = new Mock<ITransacaoService>();
            mock.Setup(t => t.Executar(It.IsAny<Action>()))
                .Callback<Action>(acao =>
                {
                    aoExecutar?.Invoke();
                    acao();
                });
            mock.Setup(t => t.Executar(It.IsAny<Func<(int, List<uint>)>>()))
                .Returns<Func<(int, List<uint>)>>(func => func());
            return mock;
        }

        [TestMethod()]
        public void CancelarInscricao_RodaDentroDeTransacao()
        {
            var mockTransacao = CriarTransacaoMock();
            var mockInscricao = new Mock<IInscricaoService>();
            mockInscricao.Setup(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns((1, new List<uint> { 10 }));

            var mockPessoa = new Mock<IPessoaService>();
            mockPessoa.Setup(s => s.GetByCpf(UsuarioTeste)).Returns(new Pessoa { Id = 7, Cpf = UsuarioTeste });

            var mockEvento = new Mock<IEventoService>();
            mockEvento.Setup(s => s.Get(1)).Returns(new Evento { Id = 1, Status = "A" });

            var mockSubevento = new Mock<ISubeventoService>();

            var controller = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
                mockEvento.Object,
                new MapperConfiguration(cfg => cfg.AddProfile(new InscricaoProfile())).CreateMapper(),
                mockInscricao.Object,
                mockPessoa.Object,
                mockSubevento.Object,
                mockTransacao.Object);

            var httpContext = new DefaultHttpContext { User = Principal() };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            controller.CancelarInscricao(1);

            mockTransacao.Verify(t => t.Executar(It.IsAny<Func<(int, List<uint>)>>()), Times.Once);
            mockEvento.Verify(e => e.AtualizarVagasDisponiveis(1, It.IsAny<bool>()), Times.Once);
            mockSubevento.Verify(s => s.AtualizarVagasDisponiveis(10, It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public void CancelarInscricao_FalhaNaTransacao_NaoLiberaVagasEMostraMensagem()
        {
            var mockTransacao = new Mock<ITransacaoService>();
            mockTransacao.Setup(t => t.Executar(It.IsAny<Func<(int, List<uint>)>>()))
                .Throws(new DbUpdateException("falha ao liberar vagas"));

            var mockInscricao = new Mock<IInscricaoService>();
            mockInscricao.Setup(s => s.CancelarInscricaoParticipante(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns((1, new List<uint> { 10 }));

            var mockPessoa = new Mock<IPessoaService>();
            mockPessoa.Setup(s => s.GetByCpf(UsuarioTeste)).Returns(new Pessoa { Id = 7, Cpf = UsuarioTeste });

            var mockEvento = new Mock<IEventoService>();
            mockEvento.Setup(s => s.Get(1)).Returns(new Evento { Id = 1, Status = "A" });

            var mockSubevento = new Mock<ISubeventoService>();

            var controller = new InscricaoController(
                null!,
                Mock.Of<ITipoInscricaoService>(),
                mockEvento.Object,
                new MapperConfiguration(cfg => cfg.AddProfile(new InscricaoProfile())).CreateMapper(),
                mockInscricao.Object,
                mockPessoa.Object,
                mockSubevento.Object,
                mockTransacao.Object);

            var httpContext = new DefaultHttpContext { User = Principal() };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var result = controller.CancelarInscricao(1);

            Assert.AreEqual("Não foi possível concluir a desistência. Tente novamente.", controller.TempData["ParticipanteMessage"]);
            mockEvento.Verify(e => e.AtualizarVagasDisponiveis(It.IsAny<uint>(), It.IsAny<bool>()), Times.Never);
            Assert.AreEqual("minhasInscricoes", ((RedirectToActionResult)result).ActionName);
        }
    }
}
