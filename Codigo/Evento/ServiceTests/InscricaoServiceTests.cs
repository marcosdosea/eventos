using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Tests
{
	[TestClass()]
	public class InscricaoServiceTests
	{
		private EventoContext _context;
		private IInscricaoService _inscricaoService;
		private MockUserManager<UsuarioIdentity> _userManager;

		[TestInitialize]
		public void Initialize()
		{
			var builder = new DbContextOptionsBuilder<EventoContext>();
			builder.UseInMemoryDatabase("Evento");
			var options = builder.Options;

			_context = new EventoContext(options);
			_context.Database.EnsureDeleted();
			_context.Database.EnsureCreated();

			var pessoas = new List<Pessoa>
			{
				new Pessoa
				{
					Id = 1,
					Nome = "João Vitor Sodré",
					NomeCracha = "Sodré",
					Cpf = "12246232367",
					Sexo = "M",
					Cep = "45340086",
					Rua = "Avenida Principal",
					Bairro = "Centro",
					Cidade = "Irece",
					Estado = "BA",
					Numero = "s/n",
					Complemento = "casa",
					Email = "email@gmail.com",
					Telefone1 = "7999900113344",
					Telefone2 = "NULL",
				},
				new Pessoa
				{
					Id = 2,
					Nome = "Nagibe Santos Wanus Junior",
					NomeCracha = "Nagibe Junior",
					Cpf = "12343455678",
					Sexo = "M",
					Cep = "45566000",
					Rua = "Rua Severino Vieira",
					Bairro = "Centro",
					Cidade = "Esplanada",
					Estado = "BA",
					Numero = "147",
					Complemento = "casa",
					Email = "nagibejr@gmail.com",
					Telefone1 = "75999643467",
					Telefone2 = "NULL",
				},
				new Pessoa
				{
					Id = 3,
					Nome = "Marcos Venicios da Palma Dias",
					NomeCracha = "Marcos Venicios",
					Cpf = "12234544667",
					Sexo = "M",
					Cep = "45340086",
					Rua = "Rua da Linha",
					Bairro = "Centro",
					Cidade = "Esplanada",
					Estado = "BA",
					Numero = "s/n",
					Complemento = "casa",
					Email = "muzanpvp@gmail.com",
					Telefone1 = "7999900113344",
					Telefone2 = "NULL",
				},
			};

			var inscricoes = new List<Inscricaopessoaevento>
			{
				new Inscricaopessoaevento
				{
					Id = 1,
					IdPessoa = 1,
					IdEvento = 1,
					IdPapel = 1,
					IdTipoInscricao = 1,
					DataInscricao = new DateTime(2024, 07, 3),
					ValorTotal = 0,
					Status = "A",
					FrequenciaFinal = 1,
					IdPessoaNavigation = pessoas[0]
				},
				new Inscricaopessoaevento
				{
					Id = 2,
					IdPessoa = 2,
					IdEvento = 1,
					IdPapel = 1,
					IdTipoInscricao = 1,
					DataInscricao = new DateTime(2024, 07, 2),
					ValorTotal = 0,
					Status = "C",
					FrequenciaFinal = 0,
					IdPessoaNavigation = pessoas[1]
				},
				new Inscricaopessoaevento
				{
					Id = 3,
					IdPessoa = 3,
					IdEvento = 1,
					IdPapel = 1,
					IdTipoInscricao = 1,
					DataInscricao = new DateTime(2024, 07, 5),
					ValorTotal = 0,
					Status = "S",
					FrequenciaFinal = 0,
					IdPessoaNavigation = pessoas[2]
				},
			};

			var eventos = new List<Evento>
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
					DataInicioInscricao = new DateTime(2024, 9, 2, 7, 30, 0),
					DataFimInscricao = new DateTime(2024, 9, 7, 12, 30, 0),
					ValorInscricao = 0,
					Website = "www.itatechjr.com.br",
					EmailEvento = "DSI@academico.ufs.br",
					EventoPublico = 1,
					Cep = "49506036",
					Estado = "SE",
					Cidade = "Itabaiana",
					Bairro = "Porto",
					Rua = "Av. Vereador Olímpio Grande",
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
					Nome = "SEMAC",
					Descricao = "Semana academica de cursos",
					DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
					DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
					InscricaoGratuita = 1,
					Status = "F",
					DataInicioInscricao = new DateTime(2024, 2, 2, 7, 30, 0),
					DataFimInscricao = new DateTime(2024, 2, 7, 12, 30, 0),
					ValorInscricao = 0,
					Website = "www.itatechjr.com.br",
					EmailEvento = "DSI@academico.ufs.br",
					EventoPublico = 1,
					Cep = "49506036",
					Estado = "SE",
					Cidade = "Itabaiana",
					Bairro = "Porto",
					Rua = "Av. Vereador Olímpio Grande",
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
					Id = 3,
					Nome = "Balada do DJ Ikaruz",
					Descricao = "Festa Fechada",
					DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
					DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
					InscricaoGratuita = 1,
					Status = "C",
					DataInicioInscricao = new DateTime(2024, 9, 2, 7, 30, 0),
					DataFimInscricao = new DateTime(2024, 9, 3, 7, 30, 0),
					ValorInscricao = 0,
					Website = "www.dj.com.br",
					EmailEvento = "DJ@gmail.com",
					EventoPublico = 1,
					Cep = "49506036",
					Estado = "SE",
					Cidade = "Itabaiana",
					Bairro = "Porto",
					Rua = "Av. Vereador Olímpio Grande",
					Numero = "s/n",
					Complemento = "Universidade",
					PossuiCertificado = 0,
					FrequenciaMinimaCertificado = 0,
					IdTipoEvento = 3,
					VagasOfertadas = 100,
					VagasReservadas = 35,
					VagasDisponiveis = 65,
					TempoMinutosReserva = 240,
					CargaHoraria = 12,
				},
			};

			var papel = new Papel
			{
				Id = 2,
				Nome = "Gestor"
			};

			_context.AddRange(pessoas);
			_context.AddRange(inscricoes);
			_context.AddRange(eventos);
			_context.Add(papel);
			_context.SaveChanges();

			_userManager = new MockUserManager<UsuarioIdentity>();
			_inscricaoService = new InscricaoService(_context, _userManager);

			var user = new UsuarioIdentity
			{
				UserName = "12246232367",
				Email = "test@example.com",
				Id = "1"
			};
			_userManager.CreateAsync(user, "Test@123").GetAwaiter().GetResult();
		}

		[TestMethod()]
		public void CreateInscricaoEventoTest()
		{
			var novaPessoa = new Pessoa
			{
				Id = 4,
				Nome = "Lucas Silva",
				NomeCracha = "Lucas",
				Cpf = "99999999999",
				Sexo = "M",
				Cep = "45340086",
				Rua = "Rua Nova",
				Bairro = "Centro",
				Cidade = "Itabaiana",
				Estado = "SE",
				Numero = "100",
				Complemento = "casa",
				Email = "lucas@gmail.com",
				Telefone1 = "79999999999",
				Telefone2 = "NULL"
			};
			_context.Pessoas.Add(novaPessoa);
			_context.SaveChanges();

			var novaInscricao = new Inscricaopessoaevento()
			{
				Id = 4,
				IdPessoa = 4,
				IdEvento = 1,
				IdPapel = 1,
				IdTipoInscricao = 1,
				DataInscricao = new DateTime(2024, 07, 5),
				ValorTotal = 0,
				Status = "S",
				FrequenciaFinal = 0,
				IdPessoaNavigation = novaPessoa
			};

			_inscricaoService.CreateInscricaoEvento(novaInscricao);

			var listaInscricao = _inscricaoService.GetByEventoAndPapel(1, 1);

			Assert.IsInstanceOfType(listaInscricao, typeof(IEnumerable<Inscricaopessoaevento>));
			Assert.IsNotNull(listaInscricao);
			Assert.AreEqual(4, listaInscricao.Count());

			var principalInscricao = listaInscricao.FirstOrDefault(i => i.Id == 1);
			Assert.IsNotNull(principalInscricao);
			Assert.AreEqual((uint)1, principalInscricao.IdPessoa);
			Assert.AreEqual((uint)1, principalInscricao.IdEvento);
			Assert.AreEqual((int)1, principalInscricao.IdPapel);
			Assert.AreEqual((uint)1, principalInscricao.IdTipoInscricao);
			Assert.AreEqual("A", principalInscricao.Status);
		}

		[TestMethod()]
		public async Task DeletePessoaPapelAsyncTest()
		{
			await _inscricaoService.DeletePessoaPapelAsync(1, 1, 1, "12246232367");

			var inscricoes = _inscricaoService.GetByEventoAndPapel(1, 1);
			Assert.IsNotNull(inscricoes);
			Assert.AreEqual(2, inscricoes.Count());
		}

		[TestMethod()]
		public void GetByEventoAndPapelTest()
		{
			var listaInscricao = _inscricaoService.GetByEventoAndPapel(1, 1);

			Assert.IsInstanceOfType(listaInscricao, typeof(IEnumerable<Inscricaopessoaevento>));
			Assert.IsNotNull(listaInscricao);
			Assert.AreEqual(3, listaInscricao.Count());

			var firstInscricao = listaInscricao.First();
			Assert.AreEqual((uint)1, firstInscricao.IdPessoa);
			Assert.AreEqual((uint)1, firstInscricao.IdEvento);
			Assert.AreEqual((int)1, firstInscricao.IdPapel);
			Assert.AreEqual((uint)1, firstInscricao.IdTipoInscricao);
			Assert.AreEqual("A", firstInscricao.Status);
		}

		[TestMethod()]
		public void GetAllTest()
		{
			var result = _inscricaoService.GetAll();
			Assert.IsNotNull(result);
			Assert.AreEqual(3, result.Count());
		}

		[TestMethod()]
		public void GetByIdTest()
		{
			var result = _inscricaoService.GetById(1);
			Assert.IsNotNull(result);
			Assert.AreEqual((uint)1, result.Id);
		}

		[TestMethod()]
		public void UpdateTest()
		{
			var inscricao = _inscricaoService.GetById(1);
			inscricao.Status = "X";
			_inscricaoService.Update(inscricao);

			var result = _inscricaoService.GetById(1);
			Assert.AreEqual("X", result.Status);
		}

		[TestMethod()]
		public void DeleteTest()
		{
			_inscricaoService.Delete(1);
			var result = _inscricaoService.GetAll();
			Assert.AreEqual(2, result.Count());
		}

		[TestMethod()]
		public void PessoaJaInscritaTest()
		{
			var result = _inscricaoService.PessoaJaInscrita(1, 1);
			Assert.IsTrue(result);
		}
	}
}