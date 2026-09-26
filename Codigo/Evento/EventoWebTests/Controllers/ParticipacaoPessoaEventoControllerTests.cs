using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using AutoMapper;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class ParticipacaoPessoaEventoControllerTests
    {
        private Mock<IParticipacaoPessoaEventoService> _mockParticipacao = null!;
        private Mock<IParticipacaoPessoaSubEventoService> _mockParticipacaoSubEvento = null!;
        private Mock<IEventoService> _mockEvento = null!;
        private Mock<ISubeventoService> _mockSubevento = null!;
        private Mock<IPessoaService> _mockPessoa = null!;
        private Mock<IInscricaoService> _mockInscricao = null!;
        private Mock<UserManager<UsuarioIdentity>> _mockUserManager = null!;
        private IMapper _mapper = null!;

        private ParticipacaoPessoaEventoController CriarController(bool comInscricaoParticipante, bool jaVinculado, int papelSeVinculado)
        {
            _mockParticipacao = new Mock<IParticipacaoPessoaEventoService>();
            _mockParticipacaoSubEvento = new Mock<IParticipacaoPessoaSubEventoService>();
            _mockEvento = new Mock<IEventoService>();
            _mockSubevento = new Mock<ISubeventoService>();
            _mockPessoa = new Mock<IPessoaService>();
            _mockInscricao = new Mock<IInscricaoService>();

            var mockUserStore = new Mock<IUserStore<UsuarioIdentity>>();
            _mockUserManager = new Mock<UserManager<UsuarioIdentity>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new UsuarioIdentity { UserName = "gestor@teste.com" });

            _mapper = new MapperConfiguration(cfg => { }).CreateMapper();

            _mockEvento.Setup(s => s.GetEventoSimpleDto(It.IsAny<uint>()))
                .Returns(new EventoSimpleDTO { Id = 1, Nome = "Evento Teste" });

            // Usuário logado é gestor do evento (permissão concedida).
            _mockInscricao.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns(new Inscricaopessoaevento());
            _mockInscricao.Setup(s => s.GetColaboradorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns((Inscricaopessoaevento)null);

            _mockPessoa.Setup(s => s.GetByCpf(It.IsAny<string>()))
                .Returns(new Pessoa { Id = 10, Nome = "Fulano", Cpf = "12345678909" });

            // Inscrição como participante (papel 4)?
            var inscricoesPapel4 = new List<Inscricaopessoaevento>();
            if (comInscricaoParticipante)
            {
                inscricoesPapel4.Add(new Inscricaopessoaevento { IdPessoa = 10, IdEvento = 1, IdPapel = 4 });
            }
            _mockInscricao.Setup(s => s.GetByEventoAndPapel(It.IsAny<uint>(), 4))
                .Returns(inscricoesPapel4);

            // Já vinculada em outro papel?
            _mockInscricao.Setup(s => s.IsInscrito(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns(jaVinculado);
            _mockInscricao.Setup(s => s.GetPapelPessoaByEvento(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns(papelSeVinculado);

            _mockParticipacao.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<Participacaopessoaevento>());
            _mockParticipacao.Setup(s => s.AddAsync(It.IsAny<Participacaopessoaevento>()))
                .ReturnsAsync(new Participacaopessoaevento());

            _mockParticipacaoSubEvento.Setup(s => s.GetBySubEvento(It.IsAny<uint>()))
                .Returns(new List<Participacaopessoasubevento>());

            _mockInscricao.Setup(s => s.CreateInscricaoEvento(It.IsAny<Inscricaopessoaevento>()))
                .Returns((uint)99);
            _mockEvento.Setup(s => s.AtualizarVagasDisponiveis(It.IsAny<uint>()));

            var controller = new ParticipacaoPessoaEventoController(
                _mockParticipacao.Object, _mockParticipacaoSubEvento.Object, _mockEvento.Object, _mockSubevento.Object,
                _mockPessoa.Object, _mockInscricao.Object, _mapper, _mockUserManager.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "gestor@teste.com"),
                new Claim(ClaimTypes.Role, "GESTOR")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            controller.TempData = new TempDataDictionary(
                controller.HttpContext, Mock.Of<ITempDataProvider>());

            return controller;
        }

        // Cenário do usuário: pessoa já cadastrada no evento (como gestora, papel 2)
        // deve gerar impedimento em ErrorMessage (vermelho), não "não está inscrita".
        [TestMethod()]
        public async Task RegistrarParticipacao_PessoaJaCadastrada_DefineErrorMessage()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: true, papelSeVinculado: 2);

            var result = await controller.RegistrarParticipacao(1, null, "123.456.789-09");

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.IsNotNull(controller.TempData["ErrorMessage"]);
            var msg = controller.TempData["ErrorMessage"]!.ToString()!;
            StringAssert.Contains(msg, "já está cadastrada");
            StringAssert.Contains(msg, "gestora");
            Assert.IsNull(controller.TempData["Message"]);
        }

        // Nova regra: pessoa cadastrada no sistema, ainda não inscrita e sem papel
        // conflitante, é inscrita automaticamente como participante e tem a entrada
        // registrada (Message verde), em vez de bloquear com "não está inscrita".
        [TestMethod()]
        public async Task RegistrarParticipacao_PessoaNaoInscrita_InscreveERegistraEntrada()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);

            var result = await controller.RegistrarParticipacao(1, null, "123.456.789-09");

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.IsNull(controller.TempData["ErrorMessage"]);
            Assert.IsNotNull(controller.TempData["Message"]);
            StringAssert.Contains(controller.TempData["Message"]!.ToString()!, "Entrada registrada");
            _mockInscricao.Verify(s => s.CreateInscricaoEvento(It.IsAny<Inscricaopessoaevento>()), Times.Once);
        }

        // Pessoa não encontrada no sistema (CPF inexistente): impedimento em vermelho.
        [TestMethod()]
        public async Task RegistrarParticipacao_PessoaNaoEncontrada_DefineErrorMessage()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockPessoa.Setup(s => s.GetByCpf(It.IsAny<string>())).Returns((Pessoa)null);

            var result = await controller.RegistrarParticipacao(1, null, "000.000.000-00");

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.IsNotNull(controller.TempData["ErrorMessage"]);
            StringAssert.Contains(controller.TempData["ErrorMessage"]!.ToString()!, "não encontrada");
            Assert.IsNull(controller.TempData["Message"]);
        }

        // Participante regularmente inscrito: registra entrada e define Message (verde).
        [TestMethod()]
        public async Task RegistrarParticipacao_ParticipanteInscrito_RegistraEntrada()
        {
            var controller = CriarController(comInscricaoParticipante: true, jaVinculado: true, papelSeVinculado: 4);

            var result = await controller.RegistrarParticipacao(1, null, "123.456.789-09");

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.IsNull(controller.TempData["ErrorMessage"]);
            Assert.IsNotNull(controller.TempData["Message"]);
            StringAssert.Contains(controller.TempData["Message"]!.ToString()!, "Entrada registrada");
        }

        [TestMethod()]
        public async Task Frequencia_IdEventoZero_ComMultiplosEventos_RedirecionaParaGerenciarEventoListar()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockEvento.Setup(s => s.GetEventByCpf(It.IsAny<string>(), 3))
                .Returns(new List<Evento> { new Evento { Id = 1 }, new Evento { Id = 2 } });

            var result = await controller.Frequencia(0, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("GerenciarEventoListar", result.ActionName);
            Assert.AreEqual("Evento", result.ControllerName);
        }

        [TestMethod()]
        public async Task Frequencia_IdEventoZero_ComEventoUnico_RedirecionaParaFrequenciaDoEvento()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockEvento.Setup(s => s.GetEventByCpf(It.IsAny<string>(), 3))
                .Returns(new List<Evento> { new Evento { Id = 42 } });

            var result = await controller.Frequencia(0, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(controller.Frequencia), result.ActionName);
            Assert.AreEqual((uint)42, result.RouteValues?["idEvento"]);
        }

        [TestMethod()]
        public async Task Frequencia_SemPermissao_Colaborador_RedirecionaParaGerenciarEventoListar()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockInscricao.Setup(s => s.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns((Inscricaopessoaevento)null);
            _mockInscricao.Setup(s => s.GetColaboradorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns((Inscricaopessoaevento)null);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "colaborador@teste.com"),
                new Claim(ClaimTypes.Role, "COLABORADOR")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var result = await controller.Frequencia(10, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("GerenciarEventoListar", result.ActionName);
            Assert.AreEqual("Evento", result.ControllerName);
        }

        [TestMethod()]
        public async Task Frequencia_ComIdSubEvento_CarregaSubEventoNoViewModel()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            var subeventoEsperado = new Subevento { Id = 7, Nome = "Workshop .NET", IdEvento = 1 };
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(subeventoEsperado);

            var result = await controller.Frequencia(1, 7) as ViewResult;

            Assert.IsNotNull(result);
            var viewModel = result.Model as FrequenciaViewModel;
            Assert.IsNotNull(viewModel);
            Assert.IsNotNull(viewModel.SubEvento);
            Assert.AreEqual((uint)7, viewModel.SubEvento.Id);
            Assert.AreEqual("Workshop .NET", viewModel.SubEvento.Nome);
            _mockSubevento.Verify(s => s.Get((uint)7), Times.Once);
        }

        [TestMethod()]
        public async Task Frequencia_ComIdSubEvento_CarregaApenasParticipacoesDoSubEvento()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 1 });
            _mockParticipacaoSubEvento.Setup(s => s.GetBySubEvento((uint)7)).Returns(new List<Participacaopessoasubevento>
            {
                new Participacaopessoasubevento
                {
                    Id = 101,
                    IdPessoa = 10,
                    IdSubEvento = 7,
                    Entrada = DateTime.Now,
                    IdPessoaNavigation = new Pessoa { Id = 10, Nome = "Participante Sub" }
                }
            });

            var result = await controller.Frequencia(1, 7) as ViewResult;

            Assert.IsNotNull(result);
            var viewModel = result.Model as FrequenciaViewModel;
            Assert.IsNotNull(viewModel);
            Assert.AreEqual(1, viewModel.Frequencias.Count());
            Assert.AreEqual((uint)101, viewModel.Frequencias.First().Id);
            Assert.AreEqual("Participante Sub", viewModel.Frequencias.First().IdPessoaNavigation.Nome);
            _mockParticipacao.Verify(s => s.GetAllAsync(), Times.Never);
        }

        [TestMethod()]
        public async Task RegistrarParticipacao_ComIdSubEvento_RegistraEntradaNoSubEvento_SemAlterarEventoPrincipal()
        {
            var controller = CriarController(comInscricaoParticipante: true, jaVinculado: true, papelSeVinculado: 4);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 1 });
            _mockParticipacaoSubEvento.Setup(s => s.GetBySubEvento((uint)7))
                .Returns(new List<Participacaopessoasubevento>());

            var result = await controller.RegistrarParticipacao(1, 7, "123.456.789-09");

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual("Entrada registrada com sucesso.", controller.TempData["Message"]);
            _mockParticipacaoSubEvento.Verify(s => s.Create(It.Is<Participacaopessoasubevento>(p => p.IdSubEvento == 7 && p.IdPessoa == 10)), Times.Once);
            _mockParticipacao.Verify(s => s.AddAsync(It.IsAny<Participacaopessoaevento>()), Times.Never);
            _mockParticipacao.Verify(s => s.UpdateAsync(It.IsAny<Participacaopessoaevento>()), Times.Never);
        }

        [TestMethod()]
        public async Task RegistrarParticipacao_ComIdSubEvento_EntradaExistenteSemSaida_RegistraSaidaNoSubEvento_SemAlterarEventoPrincipal()
        {
            var controller = CriarController(comInscricaoParticipante: true, jaVinculado: true, papelSeVinculado: 4);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 1 });
            var entradaSub = new Participacaopessoasubevento
            {
                Id = 55,
                IdPessoa = 10,
                IdSubEvento = 7,
                Entrada = DateTime.Now.AddHours(-1),
                Saida = null
            };
            _mockParticipacaoSubEvento.Setup(s => s.GetBySubEvento((uint)7))
                .Returns(new List<Participacaopessoasubevento> { entradaSub });

            var result = await controller.RegistrarParticipacao(1, 7, "123.456.789-09");

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual("Saída registrada com sucesso.", controller.TempData["Message"]);
            _mockParticipacaoSubEvento.Verify(s => s.Update(It.Is<Participacaopessoasubevento>(p => p.Id == 55 && p.Saida.HasValue)), Times.Once);
            _mockParticipacao.Verify(s => s.UpdateAsync(It.IsAny<Participacaopessoaevento>()), Times.Never);
        }

        [TestMethod()]
        public async Task ExcluirParticipacao_ComIdSubEvento_ExcluiApenasDoSubEvento()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 1 });
            _mockParticipacaoSubEvento.Setup(s => s.GetBySubEvento((uint)7)).Returns(new List<Participacaopessoasubevento>
            {
                new Participacaopessoasubevento { Id = 99, IdSubEvento = 7 }
            });

            var result = await controller.ExcluirParticipacao(99, 1, 7);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            Assert.AreEqual("Participação removida com sucesso.", controller.TempData["Message"]);
            _mockParticipacaoSubEvento.Verify(s => s.Delete((uint)99), Times.Once);
            _mockParticipacao.Verify(s => s.DeleteAsync(It.IsAny<uint>()), Times.Never);
        }

        [TestMethod()]
        public async Task Index_ComIdSubEventoDeOutroEvento_RetornaNotFound()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 999 });

            var result = await controller.Index(1, 7);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod()]
        public async Task Frequencia_ComIdSubEventoDeOutroEvento_RetornaNotFound()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 999 });

            var result = await controller.Frequencia(1, 7);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod()]
        public async Task RegistrarParticipacao_ComIdSubEventoDeOutroEvento_RetornaNotFound()
        {
            var controller = CriarController(comInscricaoParticipante: true, jaVinculado: true, papelSeVinculado: 4);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 999 });

            var result = await controller.RegistrarParticipacao(1, 7, "123.456.789-09");

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod()]
        public async Task ExcluirParticipacao_ComIdSubEventoDeOutroEvento_RetornaNotFound()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 999 });

            var result = await controller.ExcluirParticipacao(99, 1, 7);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            _mockParticipacaoSubEvento.Verify(s => s.Delete(It.IsAny<uint>()), Times.Never);
        }

        [TestMethod()]
        public async Task ExcluirParticipacao_ComParticipacaoNaoPertencenteAoSubEvento_RetornaNotFound()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockSubevento.Setup(s => s.Get((uint)7)).Returns(new Subevento { Id = 7, Nome = "Workshop", IdEvento = 1 });
            _mockParticipacaoSubEvento.Setup(s => s.GetBySubEvento((uint)7)).Returns(new List<Participacaopessoasubevento>
            {
                new Participacaopessoasubevento { Id = 88, IdSubEvento = 7 }
            });

            var result = await controller.ExcluirParticipacao(99, 1, 7);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            _mockParticipacaoSubEvento.Verify(s => s.Delete(It.IsAny<uint>()), Times.Never);
        }

        [TestMethod()]
        public async Task ExcluirParticipacao_EventoPrincipal_ComParticipacaoDeOutroEvento_RetornaNotFound()
        {
            var controller = CriarController(comInscricaoParticipante: false, jaVinculado: false, papelSeVinculado: 0);
            _mockParticipacao.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync(new Participacaopessoaevento { Id = 99, IdEvento = 999 });

            var result = await controller.ExcluirParticipacao(99, 1, null);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            _mockParticipacao.Verify(s => s.DeleteAsync(It.IsAny<uint>()), Times.Never);
        }
    }
}
