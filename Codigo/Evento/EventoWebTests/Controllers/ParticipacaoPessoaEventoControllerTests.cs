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
        private Mock<IEventoService> _mockEvento = null!;
        private Mock<ISubeventoService> _mockSubevento = null!;
        private Mock<IPessoaService> _mockPessoa = null!;
        private Mock<IInscricaoService> _mockInscricao = null!;
        private Mock<UserManager<UsuarioIdentity>> _mockUserManager = null!;
        private IMapper _mapper = null!;

        private ParticipacaoPessoaEventoController CriarController(bool comInscricaoParticipante, bool jaVinculado, int papelSeVinculado)
        {
            _mockParticipacao = new Mock<IParticipacaoPessoaEventoService>();
            _mockEvento = new Mock<IEventoService>();
            _mockSubevento = new Mock<ISubeventoService>();
            _mockPessoa = new Mock<IPessoaService>();
            _mockInscricao = new Mock<IInscricaoService>();

            var mockUserStore = new Mock<IUserStore<UsuarioIdentity>>();
            _mockUserManager = new Mock<UserManager<UsuarioIdentity>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

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

            _mockInscricao.Setup(s => s.CreateInscricaoEvento(It.IsAny<Inscricaopessoaevento>()))
                .Returns((uint)99);
            _mockEvento.Setup(s => s.AtualizarVagasDisponiveis(It.IsAny<uint>()));

            var controller = new ParticipacaoPessoaEventoController(
                _mockParticipacao.Object, _mockEvento.Object, _mockSubevento.Object,
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
    }
}
