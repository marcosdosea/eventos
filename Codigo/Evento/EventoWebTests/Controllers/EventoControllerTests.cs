using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using EventoWeb.Mappers;
using Core.Service;
using Moq;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTO;
using Microsoft.AspNetCore.Identity;

namespace EventoWeb.Controllers.Tests
{
	[TestClass()]
	public class EventoControllerTests
	{
		private static EventoController controller = null!;

		[TestInitialize]
		public void Initialize()
		{
			var mockService = new Mock<IEventoService>();
			var mockServiceEstado = new Mock<IEstadosbrasilService>();
			var mockServiceInscricao = new Mock<IInscricaoService>();
			var mockServiceTipoevento = new Mock<ITipoeventoService>();
			var mockServiceAreaInteresse = new Mock<IAreaInteresseService>();
			var mockServicePessoa = new Mock<IPessoaService>();
			var mockServiceSubevento = new Mock<ISubeventoService>();

			var mockUserStore = new Mock<IUserStore<Core.UsuarioIdentity>>();
			var mockUserManager = new Mock<UserManager<Core.UsuarioIdentity>>(
				mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

			IMapper mapper = new MapperConfiguration(cfg =>
				cfg.AddProfile(new EventoProfile())).CreateMapper();

			mockService.Setup(service => service.GetAll()).Returns(GetTestEventos());
			mockService.Setup(service => service.Get(It.IsAny<uint>())).Returns(GetTargetEvento());
			mockService.Setup(service => service.Create(It.IsAny<Evento>())).Verifiable();
            mockService.Setup(service => service.Edit(It.IsAny<Evento>(), It.IsAny<List<uint>>())).Verifiable();

            mockServiceEstado.Setup(service => service.GetAll()).Returns(new List<Estadosbrasil>());
			mockServiceTipoevento.Setup(service => service.GetAll()).Returns(new List<Tipoevento>());
			mockServiceAreaInteresse.Setup(service => service.GetAll()).Returns(new List<Areainteresse>());

			controller = new EventoController(
				mockUserManager.Object,
				mockService.Object,
				mapper,
				mockServiceEstado.Object,
				mockServiceInscricao.Object,
				mockServiceTipoevento.Object,
				mockServiceAreaInteresse.Object,
				mockServicePessoa.Object,
				mockServiceSubevento.Object
				);
		}

		[TestMethod()]
		public void IndexTest()
		{
			var result = controller.Index();
			Assert.IsInstanceOfType(result, typeof(ViewResult));
			ViewResult viewResult = (ViewResult)result;
			Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(IEnumerable<EventoModel>));
		}


		[TestMethod()]
		public void DetailsTest()
		{
			var result = controller.Details(1);
			Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
			RedirectToActionResult redirectResult = (RedirectToActionResult)result;
			Assert.AreEqual("Index", redirectResult.ActionName);
		}

		[TestMethod()]
		public void CreateTest()
		{
			var result = controller.Create();
			Assert.IsInstanceOfType(result, typeof(ViewResult));
		}

		[TestMethod()]
		public void CreateTest1()
		{
			var result = controller.Create(GetNewEvento());
			Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
			RedirectToActionResult redirectResult = (RedirectToActionResult)result;
			Assert.AreEqual("Index", redirectResult.ActionName);
		}

		[TestMethod()]
		public void EditTest()
		{
			var result = controller.Edit(1);

			if (result is RedirectToActionResult redirectResult)
			{
				Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
			}
			else
			{
				Assert.IsInstanceOfType(result, typeof(ViewResult));
				if (result is ViewResult viewResult)
				{
					Assert.IsInstanceOfType(viewResult.ViewData.Model, typeof(EventoModel));
				}
			}
		}

		[TestMethod()]
		public void EditTest1()
		{
			var result = controller.Edit(1, GetTargetEventoModel());
			Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
			RedirectToActionResult redirectResult = (RedirectToActionResult)result;
			Assert.AreEqual("Index", redirectResult.ActionName);
		}

		[TestMethod()]
		public void DeleteTest()
		{
			var result = controller.Delete(1);
			if (result is RedirectToActionResult redirectResult)
			{
				Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
				Assert.AreEqual("Index", redirectResult.ActionName);
			}
			else
			{
				Assert.IsInstanceOfType(result, typeof(ViewResult));
			}
		}

		[TestMethod()]
		public void DeleteTest1()
		{
			var result = controller.Delete((uint)1);
			Assert.IsNotNull(result);
		}

		private EventoModel GetNewEvento()
		{
			return new EventoModel
			{
				Id = 4,
				Nome = "WORKSHOP",
				Descricao = "Workshop de culinaria",
				DataInicio = new DateTime(2024, 11, 2, 7, 30, 0),
				DataFim = new DateTime(2024, 11, 7, 12, 30, 0),
				InscricaoGratuita = 1,
				Status = "A",
				DataInicioInscricao = new DateTime(2024, 10, 2, 7, 30, 0),
				DataFimInscricao = new DateTime(2024, 10, 7, 12, 30, 0),
				ValorInscricao = 0,
				Website = "www.workshop.com.br",
				EmailEvento = "workshop@gmail.com",
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

		private static Evento GetTargetEvento()
		{
			return new Evento
			{
				Id = 1,
				Nome = "SIMPOSIO",
				Descricao = "Simposio de computacao",
				DataInicio = new DateTime(2024, 12, 2, 7, 30, 0),
				DataFim = new DateTime(2024, 12, 7, 12, 30, 0),
				InscricaoGratuita = 1,
				Status = "A",
				DataInicioInscricao = new DateTime(2024, 11, 2, 7, 30, 0),
				DataFimInscricao = new DateTime(2024, 11, 7, 12, 30, 0),
				ValorInscricao = 0,
				Website = "www.simposio.com.br",
				EmailEvento = "simposio@gmail.com",
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
				Nome = "SIMPOSIO",
				Descricao = "Simposio de computacao",
				DataInicio = new DateTime(2024, 12, 2, 7, 30, 0),
				DataFim = new DateTime(2024, 12, 7, 12, 30, 0),
				InscricaoGratuita = 1,
				Status = "A",
				DataInicioInscricao = new DateTime(2024, 11, 2, 7, 30, 0),
				DataFimInscricao = new DateTime(2024, 11, 7, 12, 30, 0),
				ValorInscricao = 0,
				Website = "www.simposio.com.br",
				EmailEvento = "simposio@gmail.com",
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

		private IEnumerable<Evento> GetTestEventos()
		{
			return new List<Evento>
			{
				new Evento
				{
					Id = 1,
					Nome = "SIMPOSIO",
					Descricao = "Simposio de computacao",
					DataInicio = new DateTime(2024, 12, 2, 7, 30, 0),
					DataFim = new DateTime(2024, 12, 7, 12, 30, 0),
					InscricaoGratuita = 1,
					Status = "A",
					DataInicioInscricao = new DateTime(2024, 11, 2, 7, 30, 0),
					DataFimInscricao = new DateTime(2024, 11, 7, 12, 30, 0),
					ValorInscricao = 0,
					Website = "www.simposio.com.br",
					EmailEvento = "simposio@gmail.com",
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
					Nome = "SEMANA DE INFORMATICA",
					Descricao = "Evento para graduandos",
					DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
					DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
					InscricaoGratuita = 0,
					Status = "A",
					DataInicioInscricao = new DateTime(2024, 9, 2, 7, 30, 0),
					DataFimInscricao = new DateTime(2024, 9, 7, 12, 30, 0),
					ValorInscricao = 50,
					Website = "www.seminf.com.br",
					EmailEvento = "seminf@gmail.com",
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
					VagasReservadas = 100,
					VagasDisponiveis = 200,
					TempoMinutosReserva = 240,
					CargaHoraria = 12,
				},
			};
		}
	}
}