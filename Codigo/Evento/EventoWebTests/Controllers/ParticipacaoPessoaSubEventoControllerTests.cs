using System.Security.Claims;
using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Controllers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EventoWebTests.Controllers;

[TestClass]
public class ParticipacaoPessoaSubEventoControllerTests
{
    private readonly Mock<IParticipacaoPessoaSubEventoService> participacoes = new();
    private readonly Mock<IPessoaService> pessoas = new();
    private readonly Mock<ISubeventoService> subeventos = new();
    private readonly Mock<IInscricaoService> inscricoes = new();
    private readonly Mock<IMapper> mapper = new();
    private ParticipacaoPessoaSubEventoController controller = null!;

    [TestInitialize]
    public void Setup()
    {
        subeventos.Setup(s => s.Get(10)).Returns(new Subevento { Id = 10, IdEvento = 1, Nome = "Oficina" });
        subeventos.Setup(s => s.Get(20)).Returns(new Subevento { Id = 20, IdEvento = 2, Nome = "Outro" });
        participacoes.Setup(s => s.GetBySubEvento(It.IsAny<uint>())).Returns(Array.Empty<Participacaopessoasubevento>());
        controller = new ParticipacaoPessoaSubEventoController(participacoes.Object, pessoas.Object, subeventos.Object, mapper.Object, inscricoes.Object);
    }

    private void Login(string role)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "12345678900"), new Claim(ClaimTypes.Role, role) }, "test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    [DataTestMethod]
    [DataRow("GESTOR")]
    [DataRow("COLABORADOR")]
    public void SemVinculo_TodasActionsNegadasSemConsultarParticipacoes(string role)
    {
        Login(role);
        inscricoes.Setup(s => s.GetGestorInEvent("12345678900", 1)).Returns(new Inscricaopessoaevento());
        inscricoes.Setup(s => s.GetColaboradorInEvent("12345678900", 1)).Returns(new Inscricaopessoaevento());
        var model = new ParticipacaoPessoaSubEventoModel { Id = 1, IdSubEvento = 20 };
        var resultados = new IActionResult[] {
            controller.Index(20), controller.Details(1, 20), controller.Create(20), controller.Create(20, model),
            controller.Edit(1, 20), controller.Edit(20, 1, model), controller.Delete(1, 20), controller.DeleteConfirmed(1, 20)
        };
        foreach (var resultado in resultados) Assert.IsInstanceOfType(resultado, typeof(ForbidResult));
        participacoes.Verify(s => s.GetBySubEvento(It.IsAny<uint>()), Times.Never);
        participacoes.Verify(s => s.Create(It.IsAny<Participacaopessoasubevento>()), Times.Never);
        participacoes.Verify(s => s.Update(It.IsAny<Participacaopessoasubevento>()), Times.Never);
        participacoes.Verify(s => s.Delete(It.IsAny<uint>()), Times.Never);
    }

    [DataTestMethod]
    [DataRow("ADMINISTRADOR")]
    [DataRow("GESTOR")]
    [DataRow("COLABORADOR")]
    public void VinculadoOuAdministrador_PodeConsultar(string role)
    {
        Login(role);
        inscricoes.Setup(s => s.GetGestorInEvent("12345678900", 1)).Returns(new Inscricaopessoaevento());
        inscricoes.Setup(s => s.GetColaboradorInEvent("12345678900", 1)).Returns(new Inscricaopessoaevento());
        Assert.IsInstanceOfType(controller.Index(10), typeof(ViewResult));
        participacoes.Verify(s => s.GetBySubEvento(10), Times.Once);
    }

    [TestMethod]
    public void SubeventoInexistente_RetornaNotFound()
    {
        Login("ADMINISTRADOR");
        Assert.IsInstanceOfType(controller.Index(999), typeof(NotFoundResult));
        participacoes.Verify(s => s.GetBySubEvento(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void PostIdsDivergentes_NaoPersiste()
    {
        Login("ADMINISTRADOR");
        var model = new ParticipacaoPessoaSubEventoModel { Id = 3, IdSubEvento = 20 };
        Assert.IsInstanceOfType(controller.Create(10, model), typeof(BadRequestResult));
        Assert.IsInstanceOfType(controller.Edit(10, 3, model), typeof(BadRequestResult));
        model.IdSubEvento = 10;
        Assert.IsInstanceOfType(controller.Edit(10, 4, model), typeof(BadRequestResult));
        participacoes.Verify(s => s.Create(It.IsAny<Participacaopessoasubevento>()), Times.Never);
        participacoes.Verify(s => s.Update(It.IsAny<Participacaopessoasubevento>()), Times.Never);
    }

    [TestMethod]
    public void ParticipacaoDeOutroSubevento_NaoEditaNemExclui()
    {
        Login("ADMINISTRADOR");
        var model = new ParticipacaoPessoaSubEventoModel { Id = 3, IdSubEvento = 10 };
        Assert.IsInstanceOfType(controller.Edit(10, 3, model), typeof(NotFoundResult));
        Assert.IsInstanceOfType(controller.DeleteConfirmed(3, 10), typeof(NotFoundResult));
        participacoes.Verify(s => s.Update(It.IsAny<Participacaopessoasubevento>()), Times.Never);
        participacoes.Verify(s => s.Delete(It.IsAny<uint>()), Times.Never);
    }

    [DataTestMethod]
    [DataRow("GESTOR")]
    [DataRow("COLABORADOR")]
    [DataRow("ADMINISTRADOR")]
    public void PostValido_CriaEAtualizaParticipacaoDoSubevento(string role)
    {
        Login(role);
        inscricoes.Setup(s => s.GetGestorInEvent("12345678900", 1)).Returns(new Inscricaopessoaevento());
        inscricoes.Setup(s => s.GetColaboradorInEvent("12345678900", 1)).Returns(new Inscricaopessoaevento());
        var model = new ParticipacaoPessoaSubEventoModel { Id = 3, IdSubEvento = 10, IdPessoa = 2 };
        var entity = new Participacaopessoasubevento { Id = 3, IdSubEvento = 10, IdPessoa = 2 };
        mapper.Setup(m => m.Map<Participacaopessoasubevento>(model)).Returns(entity);
        participacoes.Setup(s => s.GetBySubEvento(10)).Returns(new[] { entity });
        Assert.IsInstanceOfType(controller.Create(10, model), typeof(RedirectToActionResult));
        Assert.IsInstanceOfType(controller.Edit(10, 3, model), typeof(RedirectToActionResult));
        participacoes.Verify(s => s.Create(entity), Times.Once);
        participacoes.Verify(s => s.Update(entity), Times.Once);
    }
}
