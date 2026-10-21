using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Helpers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using System.Text;

namespace EventoWeb.Controllers.Tests
{
    /// <summary>
    /// Cobertura da action Dashboard (Issue #727): auth GESTOR/ADMIN, view e CSV.
    /// </summary>
    [TestClass()]
    public class DashboardControllerTests
    {
        private static EventoController CriarController(
            string nomeUsuario, string[] roles,
            Mock<IEventoService> mockEvento, Mock<IInscricaoService> mockInscricao,
            Mock<ISubeventoService> mockSubevento)
        {
            var mockUserStore = new Mock<IUserStore<UsuarioIdentity>>();
            var mockUserManager = new Mock<UserManager<UsuarioIdentity>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            var controller = new EventoController(
                mockUserManager.Object, mockEvento.Object, Mock.Of<AutoMapper.IMapper>(),
                Mock.Of<IEstadosbrasilService>(), mockInscricao.Object, Mock.Of<ITipoeventoService>(),
                Mock.Of<IAreaInteresseService>(), Mock.Of<IPessoaService>(), mockSubevento.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, nomeUsuario) };
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };
            controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        private static void ConfigurarEventoOk(Mock<IEventoService> mockEvento, Mock<ISubeventoService> mockSubevento)
        {
            mockEvento.Setup(s => s.Get(1)).Returns(new Evento { Id = 1, Nome = "SEMINFO", Status = "A" });
            mockEvento.Setup(s => s.GetDashboardFinanceiro(It.IsAny<uint>(), It.IsAny<DashboardFiltrosDTO>()))
                .Returns(new DashboardFinanceiroDTO { TotalInscritos = 4 });
            mockEvento.Setup(s => s.GetDashboardOperacao(It.IsAny<uint>(), It.IsAny<DashboardFiltrosDTO>()))
                .Returns(new DashboardOperacaoDTO { Presentes = 2, Ausentes = 2 });
            mockSubevento.Setup(s => s.GetByIdEvento(1)).Returns(new List<SubeventoEventoDTO>());
        }

        [TestMethod()]
        public void Dashboard_SemVinculoNemAdmin_NegaERedirecionaGerenciar()
        {
            var mockEvento = new Mock<IEventoService>();
            var mockInscricao = new Mock<IInscricaoService>();
            var mockSubevento = new Mock<ISubeventoService>();
            ConfigurarEventoOk(mockEvento, mockSubevento);
            mockInscricao.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns((Inscricaopessoaevento)null!);

            var controller = CriarController("colab@e.com", new[] { "COLABORADOR" }, mockEvento, mockInscricao, mockSubevento);

            var result = controller.Dashboard(1, null, null, null, null, "financas", "") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("GerenciarEvento", result.ActionName);
        }

        [TestMethod()]
        public void Dashboard_AdminOk_RetornaViewComModel()
        {
            var mockEvento = new Mock<IEventoService>();
            var mockInscricao = new Mock<IInscricaoService>();
            var mockSubevento = new Mock<ISubeventoService>();
            ConfigurarEventoOk(mockEvento, mockSubevento);
            mockInscricao.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns((Inscricaopessoaevento)null!);

            var controller = CriarController("admin@e.com", new[] { "ADMINISTRADOR" }, mockEvento, mockInscricao, mockSubevento);

            var result = controller.Dashboard(1, null, null, null, null, "financas", "") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as DashboardEventoViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual((uint)1, model.IdEvento);
            Assert.AreEqual(4, model.Financeiro.TotalInscritos);
            Assert.AreEqual(2, model.Operacao.Presentes);
            Assert.IsTrue(model.CsvPreview.Count > 1);
            Assert.AreEqual(DashboardCsvExporter.Cabecalho, string.Join(";", model.CsvPreview[0]));
        }

        [TestMethod()]
        public void Dashboard_GestorComVinculo_FormatoCsv_RetornaArquivo()
        {
            var mockEvento = new Mock<IEventoService>();
            var mockInscricao = new Mock<IInscricaoService>();
            var mockSubevento = new Mock<ISubeventoService>();
            ConfigurarEventoOk(mockEvento, mockSubevento);
            mockInscricao.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns(new Inscricaopessoaevento { IdPessoa = 9, IdEvento = 1, IdPapel = 2 });

            var controller = CriarController("11111111111", new[] { "GESTOR" }, mockEvento, mockInscricao, mockSubevento);

            var result = controller.Dashboard(1, null, null, null, null, "financas", "csv") as FileContentResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("text/csv; charset=utf-8", result.ContentType);
            StringAssert.StartsWith(result.FileDownloadName, "dashboard-seminfo-1-");
            StringAssert.EndsWith(result.FileDownloadName, ".csv");

            var conteudo = Encoding.UTF8.GetString(result.FileContents).TrimStart('\uFEFF');
            StringAssert.StartsWith(conteudo, DashboardCsvExporter.Cabecalho);
        }
    }
}
