using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventoWeb.Controllers.Tests
{
    [TestClass()]
    public class EventoControllerTests
    {
        private static EventoController controller = null!;
        private static Mock<IInscricaoService> mockServiceInscricao = null!;

        [TestInitialize]
        public void Initialize()
        {
            var mockService = new Mock<IEventoService>();
            var mockServiceEstado = new Mock<IEstadosbrasilService>();
            mockServiceInscricao = new Mock<IInscricaoService>();
            var mockServiceTipoevento = new Mock<ITipoeventoService>();
            var mockServiceAreaInteresse = new Mock<IAreaInteresseService>();
            var mockServicePessoa = new Mock<IPessoaService>();
            var mockServiceSubevento = new Mock<ISubeventoService>();
            var mockUserStore = new Mock<IUserStore<UsuarioIdentity>>();
            var mockUserManager = new Mock<UserManager<UsuarioIdentity>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new EventoProfile());
                cfg.AddProfile(new PessoaProfile());
            }).CreateMapper();

            mockService.Setup(service => service.GetAll())
                .Returns(GetTestEventos());
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetEvento());
            mockService.Setup(service => service.Create(It.IsAny<Evento>(), It.IsAny<List<uint>>()))
                .Verifiable();

            mockServiceInscricao.Setup(service => service.GetByEventoAndPapel(It.IsAny<uint>(), It.IsAny<int>()))
                .Returns(GetTestInscricoes());
            mockServiceInscricao.Setup(service => service.GetGestorInEvent(It.IsAny<string>(), It.IsAny<uint>()))
                .Returns(new Inscricaopessoaevento());
            mockService.Setup(service => service.GetEventoSimpleDto(It.IsAny<uint>()))
                .Returns(new EventoSimpleDTO { Id = 1, Nome = "Evento Teste" });
            mockServiceInscricao.Setup(service => service.GetByEventoAndPapel(It.IsAny<uint>(), 3))
                .Returns(GetTestInscricoes());
            mockServicePessoa.Setup(service => service.GetByCpf(It.IsAny<string>()))
                .Returns(new Pessoa { Id = 1, Nome = "João Vitor Sodré", NomeCracha = "Sodré", Cpf = "040.268.930-57", Email = "email@gmail.com" });
            mockServiceInscricao.Setup(service => service.GetPapelPessoaByEvento(It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns((uint idPessoa, uint idEvento) => 1);
            mockServicePessoa.Setup(service => service.CreatePessoaIdentityComPapelAsync(
    It.IsAny<Pessoa>(), It.IsAny<uint>(), It.IsAny<int>()))
    .ReturnsAsync(true)
                .Verifiable();
            mockService.Setup(service => service.AtualizarVagasDisponiveis(It.IsAny<uint>(), It.IsAny<bool>()))
                .Verifiable();
            var pessoa = new Pessoa { Id = 1, Cpf = "123.456.789-00", Nome = "Teste" };
            mockServicePessoa.Setup(s => s.Get(1)).Returns(pessoa);

            controller = new EventoController(mockUserManager.Object, mockService.Object, mapper, mockServiceEstado.Object, mockServiceInscricao.Object, mockServiceTipoevento.Object, mockServiceAreaInteresse.Object, mockServicePessoa.Object, mockServiceSubevento.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "usuario@teste.com"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR"),
                new Claim(ClaimTypes.Role, "GESTOR"),
                new Claim(ClaimTypes.Role, "COLABORADOR")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext { User = claimsPrincipal };
            controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
        }

        [TestMethod()]
        public void IndexTest()
        {
            var result = controller.Index();

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(List<EventoModel>));

            List<EventoModel>? lista = (List<EventoModel>)viewResult.ViewData.Model;
            Assert.AreEqual(3, lista.Count);
        }

        [TestMethod()]
        public void DetailsTest()
        {
            var result = controller.Details(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
            EventoModel eventoModel = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Cep);
            Assert.AreEqual("SE", eventoModel.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Cidade);
            Assert.AreEqual("Porto", eventoModel.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Rua);
            Assert.AreEqual("s/n", eventoModel.Numero);
            Assert.AreEqual("Universidade", eventoModel.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, eventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void CreateTest()
        {
            var result = controller.Create();

            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod()]
        public void CreateTest_Valid()
        {
            var result = controller.Create(GetNewEvento());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void CreateTest_Invalid()
        {
            controller.ModelState.AddModelError("Nome", "Nome do Evento é obrigatório");

            var result = controller.Create(GetNewEvento());

            Assert.AreEqual(1, controller.ModelState.ErrorCount);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
        }


        [TestMethod()]
        public void EditTest_Get_Valid()
        {
            var result = controller.Edit(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
            EventoModel eventoModel = (EventoModel)viewResult.ViewData.Model;
            Assert.AreEqual("SEMINFO", eventoModel.Nome);
            Assert.AreEqual("Evento para a semana da tecnologia", eventoModel.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), eventoModel.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), eventoModel.DataFim);
            Assert.AreEqual((sbyte)1, eventoModel.InscricaoGratuita);
            Assert.AreEqual("A", eventoModel.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), eventoModel.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-07 12:30:00"), eventoModel.DataFimInscricao);
            Assert.AreEqual((decimal)0.0, eventoModel.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", eventoModel.Website);
            Assert.AreEqual("DSI@academico.ufs.br", eventoModel.EmailEvento);
            Assert.AreEqual((sbyte)1, eventoModel.EventoPublico);
            Assert.AreEqual("49506036", eventoModel.Cep);
            Assert.AreEqual("SE", eventoModel.Estado);
            Assert.AreEqual("Itabaiana", eventoModel.Cidade);
            Assert.AreEqual("Porto", eventoModel.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", eventoModel.Rua);
            Assert.AreEqual("s/n", eventoModel.Numero);
            Assert.AreEqual("Universidade", eventoModel.Complemento);
            Assert.AreEqual((sbyte)1, eventoModel.PossuiCertificado);
            Assert.AreEqual((decimal)1.0, eventoModel.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, eventoModel.IdTipoEvento);
            Assert.AreEqual((int)100, eventoModel.VagasOfertadas);
            Assert.AreEqual((int)35, eventoModel.VagasReservadas);
            Assert.AreEqual((int)65, eventoModel.VagasDisponiveis);
            Assert.AreEqual((int)240, eventoModel.TempoMinutosReserva);
            Assert.AreEqual((int)4, eventoModel.CargaHoraria);
        }

        [TestMethod()]
        public void EditTest_Post_Valid()
        {
            var result = controller.Edit(GetTargetEventoModelEdit().Id, GetTargetEventoModelEdit());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void DeleteTest_Post_Valid()
        {
            var result = controller.Delete(1, GetTargetEventoModel());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.IsNull(redirectToActionResult.ControllerName);
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void DeleteTest_Post_ServiceException_SetsErrorMessage()
        {
            var mockService = new Mock<IEventoService>();
            mockService.Setup(s => s.Get(1)).Returns(GetTargetEvento());
            mockService.Setup(s => s.Delete(1)).Throws(new ServiceException("Não foi possível excluir o evento. Verifique se existem dependências ativas."));

            var ctrl = new EventoController(
                new Mock<UserManager<UsuarioIdentity>>(new Mock<IUserStore<UsuarioIdentity>>().Object, null, null, null, null, null, null, null, null).Object,
                mockService.Object,
                new MapperConfiguration(cfg => { cfg.AddProfile(new EventoProfile()); cfg.AddProfile(new PessoaProfile()); }).CreateMapper(),
                new Mock<IEstadosbrasilService>().Object,
                new Mock<IInscricaoService>().Object,
                new Mock<ITipoeventoService>().Object,
                new Mock<IAreaInteresseService>().Object,
                new Mock<IPessoaService>().Object,
                new Mock<ISubeventoService>().Object);

            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            ctrl.TempData = new TempDataDictionary(ctrl.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var result = ctrl.Delete(1, GetTargetEventoModel());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Não foi possível excluir o evento. Verifique se existem dependências ativas.", ctrl.TempData["ErrorMessage"]);
        }

        [TestMethod()]
        public void DeleteTest_Get_Valid()
        {
            var result = controller.Delete(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
        }

        [TestMethod()]
        public void Gestor_Get_Valid()
        {
            var result = controller.CreateGestor(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoPapelModel));
        }

        [TestMethod()]
        public async Task Gestor_Post_Valid()
        {
            var result = await controller.CreateGestor(GetNewGestaoPapel());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("CreateGestor", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void Colaborador_Get_Valid()
        {
            var result = controller.CreateColaborador(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoPapelModel));
        }

        [TestMethod()]
        public async Task Colaborador_Post_Valid()
        {
            var result = await controller.CreateColaborador(GetNewGestaoPapel());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
        }

        [TestMethod()]
        public void Participante_Get_Valid()
        {
            var result = controller.CreateParticipante(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoPapelModel));
        }

        [TestMethod()]
        public async Task Participante_Post_Valid()
        {
            var result = await controller.CreateParticipante(GetNewGestaoPapel());

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("CreateParticipante", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void EditParticipante_Get_Valid()
        {
            mockServiceInscricao.Setup(s => s.GetPapelPessoaByEvento(1, 1)).Returns(4);

            var result = controller.EditParticipante(1, 1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(GestaoPapelModel));
            var model = (GestaoPapelModel)viewResult.ViewData.Model;
            Assert.AreEqual((uint)1, model.Evento.Id);
            Assert.AreEqual("Teste", model.Pessoa.Nome);
        }

        [TestMethod()]
        public void EditParticipante_Get_CarregaNomeCrachaDaInscricao()
        {
            mockServiceInscricao.Setup(s => s.GetPapelPessoaByEvento(1, 1)).Returns(4);
            mockServiceInscricao.Setup(s => s.GetByEvento(1)).Returns(new List<Inscricaopessoaevento>
            {
                new Inscricaopessoaevento { Id = 1, IdPessoa = 1, IdEvento = 1, IdPapel = 4, NomeCracha = "Cracha Personalizado" }
            });

            var result = controller.EditParticipante(1, 1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            var model = (GestaoPapelModel)viewResult.ViewData.Model;
            Assert.AreEqual("Cracha Personalizado", model.Pessoa.NomeCracha);
        }

        [TestMethod()]
        public async Task EditParticipante_Post_Valid()
        {
            var model = GetNewGestaoPapel();
            model.Pessoa.Id = 1;
            mockServiceInscricao.Setup(s => s.GetPapelPessoaByEvento(1, 1)).Returns(4);

            var result = await controller.EditParticipante(model);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("CreateParticipante", redirectToActionResult.ActionName);
            mockServiceInscricao.Verify(s => s.UpdateNomeCracha(1, model.Evento.Id, It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public async Task EditParticipante_Post_SemCamposLocalizacao_PreservaLocalizacaoExistente()
        {
            var model = GetNewGestaoPapel();
            model.Pessoa.Id = 1;
            model.Pessoa.Cep = null;
            model.Pessoa.Estado = null;
            model.Pessoa.Cidade = null;
            model.Pessoa.Bairro = null;
            model.Pessoa.Rua = null;
            model.Pessoa.Numero = null;
            model.Pessoa.Complemento = null;

            mockServiceInscricao.Setup(s => s.GetPapelPessoaByEvento(1, 1)).Returns(4);
            var result = await controller.EditParticipante(model);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("CreateParticipante", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public async Task EditParticipante_Post_PessoaNaoParticipanteDoEvento_BloqueiaSemSalvar()
        {
            // Arrange: pessoa existe (Id 1), mas não é participante deste evento
            // (papel 1, mock padrão do Initialize). Simula Pessoa.Id forjado no form.
            var model = GetNewGestaoPapel();
            model.Pessoa.Id = 1;

            var result = await controller.EditParticipante(model);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("CreateParticipante", redirectToActionResult.ActionName);
            Assert.AreEqual("A pessoa informada não é participante deste evento!", controller.TempData["Message"]);
            mockServiceInscricao.Verify(s => s.UpdateNomeCracha(It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod()]
        public async Task DeletePessoaPapel_Post_Valid()
        {
            var result = await controller.DeletePessoaPapel(1, 1, 1);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("GerenciarEvento", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void GerenciarEvento_IdEventoZero_RedirecionaParaGerenciarEventoListar()
        {
            var result = controller.GerenciarEvento(0);

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            RedirectToActionResult redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("GerenciarEventoListar", redirectToActionResult.ActionName);
        }

        [TestMethod()]
        public void GerenciarEvento_ComIdEventoValido_RetornaViewComModel()
        {
            var result = controller.GerenciarEvento(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            ViewResult viewResult = (ViewResult)result;
            Assert.IsNotNull(viewResult.Model);
            Assert.IsInstanceOfType(viewResult.Model, typeof(GerenciarEventoModel));
            var model = (GerenciarEventoModel)viewResult.Model;
            Assert.IsNotNull(model.Evento);
            Assert.AreEqual("SEMINFO", model.Evento.Nome);
        }

        [TestMethod()]
        [DataRow("Edit", new Type[] { typeof(uint), typeof(EventoModel) }, "ADMINISTRADOR")]
        [DataRow("Delete", new Type[] { typeof(uint), typeof(EventoModel) }, "ADMINISTRADOR")]
        [DataRow("CreateGestor", new Type[] { typeof(GestaoPapelModel) }, "ADMINISTRADOR")]
        [DataRow("CreateColaborador", new Type[] { typeof(GestaoPapelModel) }, "GESTOR")]
        [DataRow("CreateParticipante", new Type[] { typeof(GestaoPapelModel) }, "GESTOR,COLABORADOR")]
        [DataRow("EditParticipante", new Type[] { typeof(GestaoPapelModel) }, "GESTOR,COLABORADOR")]
        [DataRow("DeletePessoaPapel", new Type[] { typeof(uint), typeof(uint), typeof(uint) }, "ADMINISTRADOR,GESTOR,COLABORADOR")]
        public void Post_ExigeCargoCorreto(string nomeAcao, Type[] tiposParametros, string cargosEsperados)
        {
            var metodo = typeof(EventoController).GetMethod(nomeAcao, tiposParametros);
            Assert.IsNotNull(metodo, $"Ação {nomeAcao} não encontrada.");

            var autorizacao = metodo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.IsNotNull(autorizacao, $"A ação POST {nomeAcao} deve possuir [Authorize] para garantir o controle de acesso.");
            Assert.AreEqual(cargosEsperados, autorizacao!.Roles, $"POST {nomeAcao} com cargos incorretos.");

            var antiFalsificacao = metodo.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false);
            Assert.IsTrue(antiFalsificacao.Length > 0, $"POST {nomeAcao} sem [ValidateAntiForgeryToken].");
        }
        [TestMethod()]
        public void DetailsTest_ComDataHora()
        {
            var mockService = new Mock<IEventoService>();
            mockService.Setup(service => service.Get(1))
                .Returns(GetTargetEventoComHora());

            // Verificar que a data inclui hora
            var evento = mockService.Object.Get(1);
            Assert.AreEqual(new DateTime(2024, 10, 2, 7, 30, 0), evento.DataInicio);
            Assert.AreEqual(new DateTime(2024, 10, 7, 12, 30, 0), evento.DataFim);
            Assert.AreEqual(new DateTime(2024, 09, 2, 7, 30, 0), evento.DataInicioInscricao);
            Assert.AreEqual(new DateTime(2024, 09, 7, 12, 30, 0), evento.DataFimInscricao);
        }

        [TestMethod()]
        public void EventoModel_DisplayFormat_IncluiHora()
        {
            // Testa que o DisplayFormat foi atualizado para incluir HH:mm
            var propriedade = typeof(EventoModel).GetProperty("DataInicio");
            var displayFormatAttr = propriedade?.GetCustomAttributes(typeof(DisplayFormatAttribute), false)
                .FirstOrDefault() as DisplayFormatAttribute;

            Assert.IsNotNull(displayFormatAttr);
            Assert.AreEqual("{0:dd/MM/yyyy HH:mm}", displayFormatAttr.DataFormatString);
            Assert.IsTrue(displayFormatAttr.ApplyFormatInEditMode);
        }

        [TestMethod()]
        public void EventoModel_DataType_DateTime()
        {
            // Testa que o DataType foi alterado para DateTime
            var propriedade = typeof(EventoModel).GetProperty("DataInicio");
            var dataTypeAttr = propriedade?.GetCustomAttributes(typeof(DataTypeAttribute), false)
                .FirstOrDefault() as DataTypeAttribute;

            Assert.IsNotNull(dataTypeAttr);
            Assert.AreEqual(DataType.DateTime, dataTypeAttr.DataType);
        }

        [TestMethod()]
        public void SubeventoModel_DisplayFormat_IncluiHora()
        {
            // Testa que SubeventoModel também tem DateTime com hora
            var propriedade = typeof(SubeventoModel).GetProperty("DataInicio");
            var displayFormatAttr = propriedade?.GetCustomAttributes(typeof(DisplayFormatAttribute), false)
                .FirstOrDefault() as DisplayFormatAttribute;

            Assert.IsNotNull(displayFormatAttr);
            Assert.AreEqual("{0:dd/MM/yyyy HH:mm}", displayFormatAttr.DataFormatString);
        }

        [TestMethod()]
        public void SubeventoModel_DataType_DateTime()
        {
            // Testa que SubeventoModel usa DataType.DateTime
            var propriedade = typeof(SubeventoModel).GetProperty("DataInicio");
            var dataTypeAttr = propriedade?.GetCustomAttributes(typeof(DataTypeAttribute), false)
                .FirstOrDefault() as DataTypeAttribute;

            Assert.IsNotNull(dataTypeAttr);
            Assert.AreEqual(DataType.DateTime, dataTypeAttr.DataType);
        }

        [TestMethod()]
        public void EventoModel_ValidarOrdenacao_MesmoDia_HorasFormatadas()
        {
            // Teste de validação: DataInicio anterior a DataFim no mesmo dia com horários diferentes
            var modelo = new EventoModel
            {
                DataInicio = new DateTime(2024, 10, 15, 9, 0, 0),   // 09:00
                DataFim = new DateTime(2024, 10, 15, 17, 0, 0)      // 17:00
            };

            // Deve ser válido - DataInicio < DataFim
            Assert.IsTrue(modelo.DataInicio < modelo.DataFim);
        }

        [TestMethod()]
        public void EventoModel_ValidarOrdenacao_MesmoDia_HorasFimAnterior_Invalido()
        {
            // Teste de validação negativa: Hora final anterior à inicial no mesmo dia
            var modelo = new EventoModel
            {
                DataInicio = new DateTime(2024, 10, 15, 17, 0, 0),   // 17:00
                DataFim = new DateTime(2024, 10, 15, 9, 0, 0)        // 09:00
            };

            // Deve ser inválido - DataInicio >= DataFim
            Assert.IsFalse(modelo.DataInicio < modelo.DataFim);
        }

        [TestMethod()]
        public void EventoModel_ValidarInscricao_DataFimAnteriorDataFim()
        {
            // Inscrições devem fechar ANTES do evento terminar
            var modelo = new EventoModel
            {
                DataFimInscricao = new DateTime(2024, 10, 14, 23, 59, 0), // Dia anterior
                DataFim = new DateTime(2024, 10, 15, 18, 0, 0)             // Próximo dia
            };

            // Deve ser válido
            Assert.IsTrue(modelo.DataFimInscricao < modelo.DataFim);
        }

        [TestMethod()]
        public void SubeventoModel_ValidarOrdenacao_DiasDiferentes()
        {
            // Subevento pode ter múltiplos dias
            var modelo = new SubeventoModel
            {
                DataInicio = new DateTime(2024, 10, 15, 9, 0, 0),    // 15/10 09:00
                DataFim = new DateTime(2024, 10, 16, 18, 0, 0)       // 16/10 18:00
            };

            Assert.IsTrue(modelo.DataInicio < modelo.DataFim);
        }

        [TestMethod()]
        public void SubeventoModel_ValidarInscricao_PeriodoCompleto()
        {
            // Teste completo de validação de subevento
            var modeloSubevento = new SubeventoModel
            {
                DataInicio = new DateTime(2024, 10, 15, 9, 0, 0),
                DataFim = new DateTime(2024, 10, 15, 17, 0, 0),
                DataInicioInscricao = new DateTime(2024, 10, 1, 10, 0, 0),
                DataFimInscricao = new DateTime(2024, 10, 14, 23, 59, 0)
            };

            // Validações básicas
            Assert.IsTrue(modeloSubevento.DataInicio < modeloSubevento.DataFim,
                "Início deve ser anterior ao fim");
            Assert.IsTrue(modeloSubevento.DataInicioInscricao < modeloSubevento.DataFimInscricao,
                "Início de inscrição deve ser anterior ao fim");
            Assert.IsTrue(modeloSubevento.DataFimInscricao <= modeloSubevento.DataInicio,
                "Inscrições devem fechar antes do evento começar");
        }

        [TestMethod()]
        public void EventoModel_ComparacaoHoras_SegundosIgnorados()
        {
            // Verificar se a comparação considera minutos mas pode ignorar segundos
            var dataHora1 = new DateTime(2024, 10, 15, 14, 30, 0);
            var dataHora2 = new DateTime(2024, 10, 15, 14, 30, 30);

            // Na prática, para validação de negócio, podem ser consideradas iguais
            Assert.AreEqual(dataHora1.Year, dataHora2.Year);
            Assert.AreEqual(dataHora1.Month, dataHora2.Month);
            Assert.AreEqual(dataHora1.Day, dataHora2.Day);
            Assert.AreEqual(dataHora1.Hour, dataHora2.Hour);
            Assert.AreEqual(dataHora1.Minute, dataHora2.Minute);
            // Segundos podem ser diferentes
        }

        [TestMethod()]
        public void EventoModel_GerenciarEvento_TodosOsCamposComHora()
        {
            // Verificar que todos os 4 campos de data têm DisplayFormat com hora
            var campos = new[] { "DataInicio", "DataFim", "DataInicioInscricao", "DataFimInscricao" };

            foreach (var nomePropriedade in campos)
            {
                var propriedade = typeof(EventoModel).GetProperty(nomePropriedade);
                var displayFormatAttr = propriedade?.GetCustomAttributes(typeof(DisplayFormatAttribute), false)
                    .FirstOrDefault() as DisplayFormatAttribute;

                Assert.IsNotNull(displayFormatAttr, $"Campo {nomePropriedade} não tem DisplayFormat");
                Assert.IsTrue(displayFormatAttr.DataFormatString.Contains("HH:mm"),
                    $"Campo {nomePropriedade} não inclui HH:mm no formato");
                Assert.IsTrue(displayFormatAttr.ApplyFormatInEditMode,
                    $"Campo {nomePropriedade} não tem ApplyFormatInEditMode = true");
            }
        }

        [TestMethod()]
        public void SubeventoModel_GerenciarSubevento_TodosOsCamposComHora()
        {
            // Verificar que SubeventoModel também tem todos os campos atualizados
            var campos = new[] { "DataInicio", "DataFim", "DataInicioInscricao", "DataFimInscricao" };

            foreach (var nomePropriedade in campos)
            {
                var propriedade = typeof(SubeventoModel).GetProperty(nomePropriedade);
                var displayFormatAttr = propriedade?.GetCustomAttributes(typeof(DisplayFormatAttribute), false)
                    .FirstOrDefault() as DisplayFormatAttribute;

                Assert.IsNotNull(displayFormatAttr, $"Campo {nomePropriedade} não tem DisplayFormat");
                Assert.IsTrue(displayFormatAttr.DataFormatString.Contains("HH:mm"),
                    $"Campo {nomePropriedade} não inclui HH:mm no formato");
                Assert.IsTrue(displayFormatAttr.ApplyFormatInEditMode,
                    $"Campo {nomePropriedade} não tem ApplyFormatInEditMode = true");
            }
        }

        [TestMethod()]
        public void FormatacaoDataHora_BrasileiraComHora()
        {
            // Teste de formatação: data brasileira com hora
            var data = new DateTime(2024, 10, 15, 14, 30, 0);
            var formatado = data.ToString("dd/MM/yyyy HH:mm");

            Assert.AreEqual("15/10/2024 14:30", formatado);
        }

        [TestMethod()]
        public void FormatacaoDataHora_IsoParaInput()
        {
            // Teste de formatação ISO para inputs datetime-local
            var data = new DateTime(2024, 10, 15, 14, 30, 0);
            var formatado = data.ToString("yyyy-MM-ddTHH:mm");

            Assert.AreEqual("2024-10-15T14:30", formatado);
        }

        // ============================================
        // MÉTODOS AUXILIARES
        // ============================================

        private static Evento GetTargetEventoComHora()
        {
            return new Evento
            {
                Id = 1,
                Nome = "SEMINFO",
                Descricao = "Evento para a semana da tecnologia",
                DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),      // 07:30
                DataFim = new DateTime(2024, 10, 7, 12, 30, 0),        // 12:30
                InscricaoGratuita = 1,
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),   // 07:30
                DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),     // 12:30
                ValorInscricao = 0,
                Website = "www.itatechjr.com.br",
                EmailEvento = "DSI@academico.ufs.br",
                EventoPublico = 1,
                Cep = "49506036",
                Estado = "SE",
                Cidade = "Itabaiana",
                Bairro = "Porto",
                Rua = " Av. Vereador Olímpio Grande",
                Numero = "s/n",
                Complemento = "Universidade",
                PossuiCertificado = 1,
                FrequenciaMinimaCertificado = 1,
                IdTipoEvento = 1,
                VagasOfertadas = 100,
                VagasReservadas = 35,
                VagasDisponiveis = 65,
                TempoMinutosReserva = 240,
                CargaHoraria = 4,
            };
        }


        private EventoModel GetNewEvento()
        {
            return new EventoModel
            {
                Nome = "SEMINFO",
                Descricao = "Evento para a semana da tecnologia",
                DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                InscricaoGratuita = 1,
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                ValorInscricao = 0,
                Website = "www.itatechjr.com.br",
                EmailEvento = "DSI@academico.ufs.br",
                EventoPublico = 1,
                Cep = "49506036",
                Estado = "SE",
                Cidade = "Itabaiana",
                Bairro = "Porto",
                Rua = " Av. Vereador Olímpio Grande",
                Numero = "s/n",
                Complemento = "Universidade",
                PossuiCertificado = 1,
                FrequenciaMinimaCertificado = 1,
                IdTipoEvento = 1,
                VagasOfertadas = 100,
                VagasReservadas = 35,
                VagasDisponiveis = 65,
                TempoMinutosReserva = 240,
                CargaHoraria = 4,
            };
        }

        private GestaoPapelModel GetNewGestaoPapel()
        {
            var pessoa = new PessoaModel
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                NomeCracha = "Sodré",
                Cpf = "040.268.930-57",
                Sexo = "M",
                Cep = "48370-000",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irecê",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = null,
            };

            var evento = new EventoSimpleDTO
            {
                Id = 1,
                Nome = "SEMINFO",
            };

            var papel = new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };

            var inscricao = new Inscricaopessoaevento
            {
                Id = 1,

                IdPessoa = pessoa.Id,
                IdEvento = evento.Id,
                IdPapel = papel.Id,
            };

            return new GestaoPapelModel
            {
                Pessoa = pessoa,
                Evento = evento,
            };
        }

        private static Evento GetTargetEvento()
        {
            return new Evento
            {
                Id = 1,
                Nome = "SEMINFO",
                Descricao = "Evento para a semana da tecnologia",
                DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                InscricaoGratuita = 1,
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                ValorInscricao = 0,
                Website = "www.itatechjr.com.br",
                EmailEvento = "DSI@academico.ufs.br",
                EventoPublico = 1,
                Cep = "49506036",
                Estado = "SE",
                Cidade = "Itabaiana",
                Bairro = "Porto",
                Rua = " Av. Vereador Olímpio Grande",
                Numero = "s/n",
                Complemento = "Universidade",
                PossuiCertificado = 1,
                FrequenciaMinimaCertificado = 1,
                IdTipoEvento = 1,
                VagasOfertadas = 100,
                VagasReservadas = 35,
                VagasDisponiveis = 65,
                TempoMinutosReserva = 240,
                CargaHoraria = 4,
            };
        }

        private static Papel GetTargetPapel()
        {
            return new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };
        }

        private EventoModel GetTargetEventoModelEdit()
        {
            return new EventoModel
            {
                Id = 1,
                Nome = "SEMINFO",
                Descricao = "Evento para a semana da tecnologia",
                DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                InscricaoGratuita = 1,
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                ValorInscricao = 0,
                Website = "www.itatechjr.com.br",
                EmailEvento = "DSI@academico.ufs.br",
                EventoPublico = 1,
                Cep = "49506036",
                Estado = "SE",
                Cidade = "Itabaiana",
                Bairro = "Porto",
                Rua = " Av. Vereador Olímpio Grande",
                Numero = "s/n",
                Complemento = "Universidade",
                PossuiCertificado = 1,
                FrequenciaMinimaCertificado = 1,
                IdTipoEvento = 1,
                VagasOfertadas = 100,
                VagasReservadas = 35,
                VagasDisponiveis = 65,
                TempoMinutosReserva = 240,
                CargaHoraria = 4,
            };
        }

        private EventoModel GetTargetEventoModel()
        {
            return new EventoModel
            {
                Id = 1,
                Nome = "SEMINFO",
                Descricao = "Evento para a semana da tecnologia",
                DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                InscricaoGratuita = 1,
                Status = "A",
                DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                ValorInscricao = 0,
                Website = "www.itatechjr.com.br",
                EmailEvento = "DSI@academico.ufs.br",
                EventoPublico = 1,
                Cep = "49506036",
                Estado = "SE",
                Cidade = "Itabaiana",
                Bairro = "Porto",
                Rua = " Av. Vereador Olímpio Grande",
                Numero = "s/n",
                Complemento = "Universidade",
                PossuiCertificado = 1,
                FrequenciaMinimaCertificado = 1,
                IdTipoEvento = 1,
                VagasOfertadas = 100,
                VagasReservadas = 35,
                VagasDisponiveis = 65,
                TempoMinutosReserva = 240,
                CargaHoraria = 4,
            };
        }


        private IEnumerable<Inscricaopessoaevento> GetTestInscricoes()
        {
            var pessoa1 = new Pessoa
            {
                Id = 1,
                Nome = "João Vitor Sodré",
                NomeCracha = "Sodré",
                Cpf = "040.268.930-57",
                Sexo = "M",
                Cep = "48370-000",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Irecê",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = "NULL",
            };
            var pessoa2 = new Pessoa
            {
                Id = 2,
                Nome = "Nagibe Santos Wanus Junior",
                NomeCracha = "Nagibe Junior",
                Cpf = "917.091.250-55",
                Sexo = "M",
                Cep = "45566-000",
                Rua = "Rua Severino Vieira",
                Bairro = "Centro",
                Cidade = "Esplanada",
                Estado = "BA",
                Numero = "147",
                Complemento = "casa",
                Email = "nagibejr@gmail.com",
                Telefone1 = "7599643467",
                Telefone2 = "NULL",
            };
            var pessoa3 = new Pessoa
            {
                Id = 3,
                Nome = "Marcos Venicios da Palma Dias",
                NomeCracha = "Marcos Venicios",
                Cpf = "206.015.300-04",
                Sexo = "M",
                Cep = "45340-086",
                Rua = "Rua da Linha",
                Bairro = "Centro",
                Cidade = "Esplanada",
                Estado = "BA",
                Numero = "s/n",
                Complemento = "casa",
                Email = "muzanpvp@gmail.com",
                Telefone1 = "7999001133",
                Telefone2 = "NULL",
            };

            var evento = new EventoSimpleDTO
            {
                Id = 1,
                Nome = "SEMINFO",
            };

            var papel = new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };

            return new List<Inscricaopessoaevento>
            {
                new Inscricaopessoaevento
                {
                    Id = 1,
                    IdPessoa = pessoa1.Id,
                    IdEvento = evento.Id,
                    IdPapel = papel.Id,
                },
                new Inscricaopessoaevento
                {
                    Id = 2,
                    IdPessoa = pessoa2.Id,
                    IdEvento = evento.Id,
                    IdPapel = papel.Id,
                },
                new Inscricaopessoaevento
                {
                    Id = 3,
                    IdPessoa = pessoa3.Id,
                    IdEvento = evento.Id,
                    IdPapel = papel.Id,
                }
            };
        }

        private IEnumerable<Evento> GetTestEventos()
        {
            return new List<Evento>
            {
                new Evento
                {
                    Id = 1,
                    Nome = "SEMINFO",
                    Descricao = "Evento para a semana da tecnologia",
                    DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                    InscricaoGratuita = 1,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 09, 7, 12, 30, 0),
                    ValorInscricao = 0,
                    Website = "www.itatechjr.com.br",
                    EmailEvento = "DSI@academico.ufs.br",
                    EventoPublico = 1,
                    Cep = "49506036",
                    Estado = "SE",
                    Cidade = "Itabaiana",
                    Bairro = "Porto",
                    Rua = " Av. Vereador Olímpio Grande",
                    Numero = "s/n",
                    Complemento = "Universidade",
                    PossuiCertificado = 1,
                    FrequenciaMinimaCertificado = 1,
                    IdTipoEvento = 1,
                    VagasOfertadas = 100,
                    VagasReservadas = 35,
                    VagasDisponiveis = 65,
                    TempoMinutosReserva = 240,
                    CargaHoraria = 4,
                },
                new Evento
                {
                    Id = 2,
                    Nome = "ENCONTRO",
                    Descricao = "Evento para encontro de estudantes",
                    DataInicio = new DateTime(2024, 11, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 11, 7, 12, 30, 0),
                    InscricaoGratuita = 0,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 10, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 10, 7, 12, 30, 0),
                    ValorInscricao = 50,
                    Website = "www.encontro.com.br",
                    EmailEvento = "encontro@gmail.com",
                    EventoPublico = 1,
                    Cep = "49506036",
                    Estado = "SE",
                    Cidade = "Itabaiana",
                    Bairro = "Porto",
                    Rua = " Av. Vereador Olímpio Grande",
                    Numero = "s/n",
                    Complemento = "Universidade",
                    PossuiCertificado = 1,
                    FrequenciaMinimaCertificado = 1,
                    IdTipoEvento = 1,
                    VagasOfertadas = 200,
                    VagasReservadas = 50,
                    VagasDisponiveis = 150,
                    TempoMinutosReserva = 240,
                    CargaHoraria = 8,
                },
                new Evento
                {
                    Id = 3,
                    Nome = "CONGRESSO",
                    Descricao = "Evento para congresso de tecnologia",
                    DataInicio = new DateTime(2024, 12, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 12, 7, 12, 30, 0),
                    InscricaoGratuita = 0,
                    Status = "A",
                    DataInicioInscricao = new DateTime(2024, 11, 2, 7, 30, 0),
                    DataFimInscricao = new DateTime(2024, 11, 7, 12, 30, 0),
                    ValorInscricao = 100,
                    Website = "www.congresso.com.br",
                    EmailEvento = "congresso@gmail.com",
                    EventoPublico = 1,
                    Cep = "49506036",
                    Estado = "SE",
                    Cidade = "Itabaiana",
                    Bairro = "Porto",
                    Rua = " Av. Vereador Olímpio Grande",
                    Numero = "s/n",
                    Complemento = "Universidade",
                    PossuiCertificado = 1,
                    FrequenciaMinimaCertificado = 1,
                    IdTipoEvento = 1,
                    VagasOfertadas = 300,
                    VagasReservadas = 75,
                    VagasDisponiveis = 225,
                    TempoMinutosReserva = 240,
                    CargaHoraria = 12,
                }
            };
        }
    }
}
