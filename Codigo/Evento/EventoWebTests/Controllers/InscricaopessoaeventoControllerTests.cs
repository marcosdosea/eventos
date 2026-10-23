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
    /// A exclusão administrativa deve apenas delegar ao service; a exclusão e o
    /// recálculo de vagas acontecem de forma atômica na camada Service (Issue #861).
    /// </summary>
    [TestClass()]
    public class InscricaopessoaeventoControllerTests
    {
        [TestMethod()]
        public void DeleteConfirmed_DelegaExclusaoAoService()
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

            var controller = new InscricaopessoaeventoController(
                mockService.Object,
                Mock.Of<IPessoaService>(),
                mockEvento.Object,
                Mock.Of<ITipoInscricaoService>(),
                Mock.Of<ILogger<InscricaopessoaeventoController>>(),
                Mock.Of<IMapper>());

            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var result = controller.DeleteConfirmed(5);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            mockService.Verify(s => s.Delete(5), Times.Once);
            mockEvento.Verify(e => e.AtualizarVagasDisponiveis(It.IsAny<uint>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
