using AutoMapper;
using Core;
using Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EventoWeb.Controllers.Tests
{
    /// <summary>
    /// Garante que a exclusão administrativa de uma inscrição libera a vaga
    /// do evento dentro da mesma transação (Issue #861).
    /// </summary>
    [TestClass()]
    public class InscricaopessoaeventoControllerTests
    {
        [TestMethod()]
        public void DeleteConfirmed_RecalculaVagasDisponiveisDentroDaTransacao()
        {
            var inscricao = new Inscricaopessoaevento
            {
                Id = 5,
                IdEvento = 1,
                IdPessoa = 7,
                IdPapel = 4
            };

            var mockService = new Mock<IInscricaopessoaeventoService>();
            mockService.Setup(s => s.GetById(5)).Returns(inscricao);

            var mockEvento = new Mock<IEventoService>();
            var mockTransacao = new Mock<ITransacaoService>();

            var dentroDaTransacao = false;
            var deleteExecutado = false;

            mockService.Setup(s => s.Delete(5))
                .Callback(() => deleteExecutado = true);

            mockEvento.Setup(e => e.AtualizarVagasDisponiveis(1, It.IsAny<bool>()))
                .Callback(() =>
                {
                    Assert.IsTrue(dentroDaTransacao, "O recálculo de vagas deve ocorrer dentro da transação.");
                    Assert.IsTrue(deleteExecutado, "O recálculo deve ocorrer após a exclusão da inscrição.");
                });

            mockTransacao.Setup(t => t.Executar(It.IsAny<Action>()))
                .Callback<Action>(acao =>
                {
                    dentroDaTransacao = true;
                    try
                    {
                        acao();
                    }
                    finally
                    {
                        dentroDaTransacao = false;
                    }
                });

            var controller = new InscricaopessoaeventoController(
                mockService.Object,
                Mock.Of<IPessoaService>(),
                mockEvento.Object,
                Mock.Of<ITipoInscricaoService>(),
                Mock.Of<ILogger<InscricaopessoaeventoController>>(),
                Mock.Of<IMapper>(),
                mockTransacao.Object);

            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var result = controller.DeleteConfirmed(5);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            mockService.Verify(s => s.Delete(5), Times.Once);
            mockEvento.Verify(e => e.AtualizarVagasDisponiveis(1, It.IsAny<bool>()), Times.Once);
            mockTransacao.Verify(t => t.Executar(It.IsAny<Action>()), Times.Once);
        }
    }
}
