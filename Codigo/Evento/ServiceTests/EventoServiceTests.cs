using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Tests
{
    [TestClass()]
    public class EventoServiceTests
    {
        private EventoContext _context;
        private IEventoService _eventoService;

        [TestInitialize]
        public void Initialize()
        {
            //Arrange
            var builder = new DbContextOptionsBuilder<EventoContext>();
            builder.UseInMemoryDatabase("Evento");
            var options = builder.Options;

            _context = new EventoContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

			var areainteresses = new List<Areainteresse>
				{
				new Areainteresse
				{
						Id = 1,
						Nome =  "Curso",
					},
				new Areainteresse
				{
						Id = 2,
						Nome =  "Palestra",
					},
				new Areainteresse
				{
						Id = 3,
						Nome =  "Festival",
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
                        Nome = "SEMAC",
                        Descricao = "Semana academica de cursos",
                        DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                    DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                        InscricaoGratuita = 1,
                        Status = "F",
                        DataInicioInscricao = new DateTime(2024, 02, 2, 7, 30, 0),
                        DataFimInscricao = new DateTime(2024, 02, 7, 12, 30, 0),
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
                        Id = 3,
                        Nome = "Balada do DJ Ikaruz",
                        Descricao = "Festa Fechada",
                        DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                        DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                        InscricaoGratuita = 1,
                        Status = "C",
                        DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                        DataFimInscricao = new DateTime(2024, 09, 3, 7, 30, 0),
                        ValorInscricao = 0,
                        Website = "www.dj.com.br",
                        EmailEvento = "DJ@gmail.com",
                        EventoPublico = 1,
                        Cep = "49506036",
                        Estado = "SE",
                        Cidade = "Itabaiana",
                        Bairro = "Porto",
                        Rua = " Av. Vereador Olímpio Grande",
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

            _context.AddRange(eventos);
			_context.AddRange(areainteresses);

			_context.SaveChanges();

            _eventoService = new EventoService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _eventoService.Create(new Evento()
            {
                Id = 4,
                Nome = "Balada do DJ Ikaruz",
                Descricao = "Festa Fechada",
                DataInicio = new DateTime(2024, 10, 2, 7, 30, 0),
                DataFim = new DateTime(2024, 10, 7, 12, 30, 0),
                InscricaoGratuita = 1,
                Status = "C",
                DataInicioInscricao = new DateTime(2024, 09, 2, 7, 30, 0),
                DataFimInscricao = new DateTime(2024, 09, 3, 7, 30, 0),
                ValorInscricao = 0,
                Website = "www.dj.com.br",
                EmailEvento = "DJ@gmail.com",
                EventoPublico = 1,
                Cep = "49506036",
                Estado = "SE",
                Cidade = "Itabaiana",
                Bairro = "Porto",
                Rua = " Av. Vereador Olímpio Grande",
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
            });
            // Assert
            Assert.AreEqual(4, _eventoService.GetAll().Count());
            var evento = _eventoService.Get(4);
            Assert.AreEqual("Balada do DJ Ikaruz", evento.Nome);
            Assert.AreEqual("Festa Fechada", evento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), evento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), evento.DataFim);
            Assert.AreEqual((sbyte)1, evento.InscricaoGratuita);
            Assert.AreEqual("C", evento.Status);
            Assert.AreEqual(DateTime.Parse("2024-09-02 07:30:00"), evento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-09-03 07:30:00"), evento.DataFimInscricao);
            Assert.AreEqual((decimal)0, evento.ValorInscricao);
            Assert.AreEqual("www.dj.com.br", evento.Website);
            Assert.AreEqual("DJ@gmail.com", evento.EmailEvento);
            Assert.AreEqual((sbyte)1, evento.EventoPublico);
            Assert.AreEqual("49506036", evento.Cep);
            Assert.AreEqual("SE", evento.Estado);
            Assert.AreEqual("Itabaiana", evento.Cidade);
            Assert.AreEqual("Porto", evento.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", evento.Rua);
            Assert.AreEqual("s/n", evento.Numero);
            Assert.AreEqual("Universidade", evento.Complemento);
            Assert.AreEqual((sbyte)0, evento.PossuiCertificado);
            Assert.AreEqual((decimal)0, evento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)3, evento.IdTipoEvento);
            Assert.AreEqual((int)100, evento.VagasOfertadas);
            Assert.AreEqual((int)35, evento.VagasReservadas);
            Assert.AreEqual((int)65, evento.VagasDisponiveis);
            Assert.AreEqual((int)240, evento.TempoMinutosReserva);
            Assert.AreEqual((int)12, evento.CargaHoraria);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _eventoService.Delete(1);
            // Assert
            Assert.AreEqual(2, _eventoService.GetAll().Count());
            var evento = _eventoService.Get(1);
            Assert.AreEqual(null, evento);
        }

        [TestMethod()]
        public void DeleteTest_WithDependencies_DeletesSuccessfully()
        {
            // Arrange: criar subevento, tipo de inscricao, inscricao e pagamento para o evento 2
            var subevento = new Subevento
            {
                Id = 10,
                IdEvento = 2,
                Nome = "Workshop C#",
                Descricao = "Workshop",
                Status = "A",
                DataInicio = DateTime.Now,
                DataFim = DateTime.Now.AddDays(1),
                DataInicioInscricao = DateTime.Now,
                DataFimInscricao = DateTime.Now.AddDays(1),
                IdTipoEvento = 1
            };
            var tipoInscricao = new Tipoinscricao
            {
                Id = 10,
                IdEvento = 2,
                Nome = "Geral",
                Descricao = "Geral",
                DataInicio = DateTime.Now,
                Datafim = DateTime.Now.AddDays(1)
            };
            var inscricao = new Inscricaopessoaevento
            {
                Id = 10,
                IdEvento = 2,
                IdPessoa = 1,
                IdPapel = 4,
                Status = "A"
            };
            var pagamento = new Pagamento
            {
                Id = 10,
                IdInscricaoPessoaEvento = 10,
                Status = "C",
                Forma = "P"
            };

            _context.Subeventos.Add(subevento);
            _context.Tipoinscricaos.Add(tipoInscricao);
            _context.Inscricaopessoaeventos.Add(inscricao);
            _context.Pagamentos.Add(pagamento);
            _context.SaveChanges();

            // Act
            _eventoService.Delete(2);

            // Assert
            Assert.IsNull(_eventoService.Get(2));
            Assert.IsFalse(_context.Subeventos.Any(s => s.Id == 10));
            Assert.IsFalse(_context.Tipoinscricaos.Any(t => t.Id == 10));
            Assert.IsFalse(_context.Inscricaopessoaeventos.Any(i => i.Id == 10));
            Assert.IsFalse(_context.Pagamentos.Any(p => p.Id == 10));
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var evento = _eventoService.Get(2);
            evento.Id = 2;
            evento.Nome = "SEMAC";
            evento.Descricao = "Semana academica de cursos";
            evento.DataInicio = new DateTime(2024, 10, 1, 7, 30, 0);
            evento.DataFim = new DateTime(2024, 10, 7, 12, 30, 0);
            evento.InscricaoGratuita = 1;
            evento.Status = "F";
            evento.DataInicioInscricao = new DateTime(2024, 02, 1, 7, 30, 0);
            evento.DataFimInscricao = new DateTime(2024, 02, 7, 12, 30, 0);
            evento.ValorInscricao = 0;
            evento.Website = "www.itatechjr.com.br";
            evento.EmailEvento = "DSI@academico.ufs.br";
            evento.EventoPublico = 1;
            evento.Cep = "49506036";
            evento.Estado = "SE";
            evento.Cidade = "Itabaiana";
            evento.Bairro = "Porto";
            evento.Rua = " Av. Vereador Olímpio Grande";
            evento.Numero = "s/n";
            evento.Complemento = "Universidade";
            evento.PossuiCertificado = 1;
            evento.FrequenciaMinimaCertificado = 1;
            evento.IdTipoEvento = 1;
            evento.VagasOfertadas = 100;
            evento.VagasReservadas = 35;
            evento.VagasDisponiveis = 65;
            evento.TempoMinutosReserva = 240;
            evento.CargaHoraria = 4;
			var IdsAreaInteresse = new List<uint> {1, 3};

			// Act
			_eventoService.Edit(evento, IdsAreaInteresse);
            //Assert
            Assert.AreEqual((uint)2, evento.Id);
            Assert.AreEqual("SEMAC", evento.Nome);
            Assert.AreEqual("Semana academica de cursos", evento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-01 07:30:00"), evento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), evento.DataFim);
            Assert.AreEqual((sbyte)1, evento.InscricaoGratuita);
            Assert.AreEqual("F", evento.Status);
            Assert.AreEqual(new DateTime(2024, 02, 1, 7, 30, 0), evento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-02-07 12:30:00"), evento.DataFimInscricao);
            Assert.AreEqual((decimal)0, evento.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", evento.Website);
            Assert.AreEqual("DSI@academico.ufs.br", evento.EmailEvento);
            Assert.AreEqual((sbyte)1, evento.EventoPublico);
            Assert.AreEqual("49506036", evento.Cep);
            Assert.AreEqual("SE", evento.Estado);
            Assert.AreEqual("Itabaiana", evento.Cidade);
            Assert.AreEqual("Porto", evento.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", evento.Rua);
            Assert.AreEqual("s/n", evento.Numero);
            Assert.AreEqual("Universidade", evento.Complemento);
            Assert.AreEqual((sbyte)1, evento.PossuiCertificado);
            Assert.AreEqual((decimal)1, evento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, evento.IdTipoEvento);
            Assert.AreEqual((int)100, evento.VagasOfertadas);
            Assert.AreEqual((int)35, evento.VagasReservadas);
            Assert.AreEqual((int)65, evento.VagasDisponiveis);
            Assert.AreEqual((int)240, evento.TempoMinutosReserva);
            Assert.AreEqual((int)4, evento.CargaHoraria);
        }

        [TestMethod()]
        public void GetTest()
        {
            var evento = _eventoService.Get(2);
            Assert.AreEqual((uint)2, evento.Id);
            Assert.AreEqual("SEMAC", evento.Nome);
            Assert.AreEqual("Semana academica de cursos", evento.Descricao);
            Assert.AreEqual(DateTime.Parse("2024-10-02 07:30:00"), evento.DataInicio);
            Assert.AreEqual(DateTime.Parse("2024-10-07 12:30:00"), evento.DataFim);
            Assert.AreEqual((sbyte)1, evento.InscricaoGratuita);
            Assert.AreEqual("F", evento.Status);
            Assert.AreEqual(DateTime.Parse("2024-02-02 07:30:00"), evento.DataInicioInscricao);
            Assert.AreEqual(DateTime.Parse("2024-02-07 12:30:00"), evento.DataFimInscricao);
            Assert.AreEqual((decimal)0, evento.ValorInscricao);
            Assert.AreEqual("www.itatechjr.com.br", evento.Website);
            Assert.AreEqual("DSI@academico.ufs.br", evento.EmailEvento);
            Assert.AreEqual((sbyte)1, evento.EventoPublico);
            Assert.AreEqual("49506036", evento.Cep);
            Assert.AreEqual("SE", evento.Estado);
            Assert.AreEqual("Itabaiana", evento.Cidade);
            Assert.AreEqual("Porto", evento.Bairro);
            Assert.AreEqual(" Av. Vereador Olímpio Grande", evento.Rua);
            Assert.AreEqual("s/n", evento.Numero);
            Assert.AreEqual("Universidade", evento.Complemento);
            Assert.AreEqual((sbyte)1, evento.PossuiCertificado);
            Assert.AreEqual((decimal)1, evento.FrequenciaMinimaCertificado);
            Assert.AreEqual((uint)1, evento.IdTipoEvento);
            Assert.AreEqual((int)100, evento.VagasOfertadas);
            Assert.AreEqual((int)35, evento.VagasReservadas);
            Assert.AreEqual((int)65, evento.VagasDisponiveis);
            Assert.AreEqual((int)240, evento.TempoMinutosReserva);
            Assert.AreEqual((int)4, evento.CargaHoraria);
        }

        [TestMethod()]
        public void GetEventoSimpleDtoTest()
        {
            var evento = _eventoService.GetEventoSimpleDto(2);
            Assert.IsNotNull(evento);
            Assert.AreEqual((uint)2, evento.Id);
            Assert.AreEqual("SEMAC", evento.Nome);
        }

        [TestMethod()]
        public void GetEventoSimpleDtoInexistenteTest()
        {
            var evento = _eventoService.GetEventoSimpleDto(999);
            Assert.IsNull(evento);
        }

        [TestMethod()]
        public void GetAllSimpleDtoTest()
        {
            // Act
            var listaEvento = _eventoService.GetAllSimpleDto();

            // Assert
            Assert.IsNotNull(listaEvento);
            Assert.AreEqual(3, listaEvento.Count());
            // Deve estar ordenado por Nome ("Balada do DJ Ikaruz", "SEMAC", "SEMINFO")
            var listaOrdenada = listaEvento.ToList();
            Assert.AreEqual("Balada do DJ Ikaruz", listaOrdenada[0].Nome);
            Assert.AreEqual("SEMAC", listaOrdenada[1].Nome);
            Assert.AreEqual("SEMINFO", listaOrdenada[2].Nome);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaEvento = _eventoService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaEvento, typeof(IEnumerable<Evento>));
            Assert.IsNotNull(listaEvento);
            Assert.AreEqual(3, listaEvento.Count());
            Assert.AreEqual((uint)1, listaEvento.First().Id);
        }

        [TestMethod()]
        public void GetByNomeTest()
        {
            var evento = _eventoService.GetByNome("SEMAC");
            Assert.IsInstanceOfType(evento, typeof(IEnumerable<EventoDTO>));
            Assert.IsNotNull(evento);
            Assert.AreEqual(1, evento.Count());
            Assert.AreEqual((uint)2, evento.First().Id);
            Assert.AreEqual("SEMAC", evento.First().Nome);
        }

        [TestMethod()]
        public void GetNomeByIdTest()
        {
            var evento = _eventoService.GetNomeById(2);
            Assert.AreEqual("SEMAC", evento);
        }

        [TestMethod()]
        public void GetEventByCpfTest()
        {
            // Arrange
            var pessoa = new Pessoa
            {
                Id = 10,
                Nome = "Luiz Augusto",
                NomeCracha = "Luiz",
                Cpf = "12345678901",
                Email = "luiz@exemplo.com",
                Cep = "49500000",
                Estado = "SE",
                Cidade = "Itabaiana",
                Bairro = "Centro",
                Rua = "Rua A",
                Numero = "100"
            };
            var inscricao = new Inscricaopessoaevento
            {
                IdPessoa = 10,
                IdEvento = 1,
                IdPapel = 2,
                DataInscricao = DateTime.Now,
                Status = "A"
            };
            _context.Pessoas.Add(pessoa);
            _context.Inscricaopessoaeventos.Add(inscricao);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();

            // Act
            var eventos = _eventoService.GetEventByCpf("12345678901", 2);

            // Assert
            Assert.IsNotNull(eventos);
            Assert.AreEqual(1, eventos.Count());
            Assert.AreEqual((uint)1, eventos.First().Id);
            Assert.AreEqual("SEMINFO", eventos.First().Nome);
            Assert.AreEqual(EntityState.Detached, _context.Entry(eventos.First()).State);
        }

        [TestMethod()]
        [ExpectedException(typeof(ServiceException))]
        public void GetEventByCpfPessoaNaoEncontradaTest()
        {
            _eventoService.GetEventByCpf("00000000000", 2);
        }

        [TestMethod()]
        public void GetAreasInteresseByEventoIdTest()
        {
            // Arrange: associa area de interesse 1 ao evento 1
            var evento = _context.Eventos.Include(e => e.IdAreaInteresses).First(e => e.Id == 1);
            var area = _context.Areainteresses.First(a => a.Id == 1);
            evento.IdAreaInteresses.Add(area);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();

            // Act
            var areas = _eventoService.GetAreasInteresseByEventoId(1);

            // Assert
            Assert.IsNotNull(areas);
            Assert.AreEqual(1, areas.Count());
            Assert.AreEqual("Curso", areas.First().Nome);
            Assert.AreEqual(EntityState.Detached, _context.Entry(areas.First()).State);
        }

        [TestMethod()]
        public void SearchComFiltroPossuiSubeventosAtivoRetornaApenasEventosComSubeventosTest()
        {
            // Arrange
            var eventoComSub = new Evento
            {
                Id = 100,
                Nome = "Congresso de Tecnologia",
                Descricao = "Grande congresso com varias atividades",
                Status = "A",
                DataInicioInscricao = DateTime.Now.AddDays(-5),
                DataFimInscricao = DateTime.Now.AddDays(10),
                DataInicio = DateTime.Now.AddDays(15),
                DataFim = DateTime.Now.AddDays(20),
                IdTipoEvento = 1,
                VagasOfertadas = 100,
                VagasDisponiveis = 100,
                CargaHoraria = 20,
                TempoMinutosReserva = 15,
                PossuiCertificado = 1,
                InscricaoGratuita = 1
            };

            var eventoSemSub = new Evento
            {
                Id = 101,
                Nome = "Palestra Unica",
                Descricao = "Palestra simples sem subeventos",
                Status = "A",
                DataInicioInscricao = DateTime.Now.AddDays(-5),
                DataFimInscricao = DateTime.Now.AddDays(10),
                DataInicio = DateTime.Now.AddDays(15),
                DataFim = DateTime.Now.AddDays(20),
                IdTipoEvento = 1,
                VagasOfertadas = 50,
                VagasDisponiveis = 50,
                CargaHoraria = 2,
                TempoMinutosReserva = 15,
                PossuiCertificado = 1,
                InscricaoGratuita = 1
            };

            var subevento = new Subevento
            {
                Id = 50,
                IdEvento = 100,
                Nome = "Minicurso de IA",
                Descricao = "Minicurso pratico",
                Status = "A",
                DataInicioInscricao = DateTime.Now.AddDays(-5),
                DataFimInscricao = DateTime.Now.AddDays(10),
                DataInicio = DateTime.Now.AddDays(15),
                DataFim = DateTime.Now.AddDays(16),
                IdTipoEvento = 1,
                VagasOfertadas = 30,
                VagasDisponiveis = 30,
                CargaHoraria = 4,
                PossuiCertificado = 1,
                InscricaoGratuita = 1
            };

            _context.Eventos.AddRange(eventoComSub, eventoSemSub);
            _context.Subeventos.Add(subevento);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();

            var filtro = new EventoFilterDTO
            {
                PossuiSubeventos = true
            };

            // Act
            int totalRegistros;
            var resultados = _eventoService.Search(filtro, 1, 10, out totalRegistros).ToList();

            // Assert
            Assert.AreEqual(1, totalRegistros);
            Assert.AreEqual(1, resultados.Count);
            Assert.AreEqual((uint)100, resultados.First().Id);
            Assert.AreEqual("Congresso de Tecnologia", resultados.First().Nome);
        }

        [TestMethod()]
        public void SearchSemFiltroPossuiSubeventosRetornaTodosEventosAtivosTest()
        {
            // Arrange
            var eventoComSub = new Evento
            {
                Id = 200,
                Nome = "Congresso de Saude",
                Descricao = "Congresso com minicursos",
                Status = "A",
                DataInicioInscricao = DateTime.Now.AddDays(-5),
                DataFimInscricao = DateTime.Now.AddDays(10),
                DataInicio = DateTime.Now.AddDays(15),
                DataFim = DateTime.Now.AddDays(20),
                IdTipoEvento = 1,
                VagasOfertadas = 100,
                VagasDisponiveis = 100,
                CargaHoraria = 20,
                TempoMinutosReserva = 15,
                PossuiCertificado = 1,
                InscricaoGratuita = 1
            };

            var eventoSemSub = new Evento
            {
                Id = 201,
                Nome = "Palestra de Nutricao",
                Descricao = "Palestra sem subatividades",
                Status = "A",
                DataInicioInscricao = DateTime.Now.AddDays(-5),
                DataFimInscricao = DateTime.Now.AddDays(10),
                DataInicio = DateTime.Now.AddDays(15),
                DataFim = DateTime.Now.AddDays(20),
                IdTipoEvento = 1,
                VagasOfertadas = 50,
                VagasDisponiveis = 50,
                CargaHoraria = 2,
                TempoMinutosReserva = 15,
                PossuiCertificado = 1,
                InscricaoGratuita = 1
            };

            var subevento = new Subevento
            {
                Id = 60,
                IdEvento = 200,
                Nome = "Workshop Pratico",
                Descricao = "Workshop",
                Status = "A",
                DataInicioInscricao = DateTime.Now.AddDays(-5),
                DataFimInscricao = DateTime.Now.AddDays(10),
                DataInicio = DateTime.Now.AddDays(15),
                DataFim = DateTime.Now.AddDays(16),
                IdTipoEvento = 1,
                VagasOfertadas = 30,
                VagasDisponiveis = 30,
                CargaHoraria = 4,
                PossuiCertificado = 1,
                InscricaoGratuita = 1
            };

            _context.Eventos.AddRange(eventoComSub, eventoSemSub);
            _context.Subeventos.Add(subevento);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();

            var filtro = new EventoFilterDTO
            {
                PossuiSubeventos = false
            };

            // Act
            int totalRegistros;
            var resultados = _eventoService.Search(filtro, 1, 10, out totalRegistros).ToList();

            // Assert: deve incluir ambos os eventos ativos
            Assert.IsTrue(totalRegistros >= 2);
            Assert.IsTrue(resultados.Any(e => e.Id == 200));
            Assert.IsTrue(resultados.Any(e => e.Id == 201));
        }

        [TestMethod()]
        public void GetEventosVitrine_GaranteExclusaoDeEventosComStatusNaoAtivo()
        {
            // Arrange
            var eventoAtivo = new Evento
            {
                Id = 301,
                Nome = "Evento Ativo",
                Status = "A",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-2),
                DataFimInscricao = DateTime.Now.AddDays(5),
                DataInicio = DateTime.Now.AddDays(6),
                DataFim = DateTime.Now.AddDays(10)
            };
            var eventoCadastro = new Evento
            {
                Id = 302,
                Nome = "Evento Cadastro",
                Status = "C",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-2),
                DataFimInscricao = DateTime.Now.AddDays(5),
                DataInicio = DateTime.Now.AddDays(6),
                DataFim = DateTime.Now.AddDays(10)
            };
            var eventoInativo = new Evento
            {
                Id = 303,
                Nome = "Evento Inativo",
                Status = "I",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-2),
                DataFimInscricao = DateTime.Now.AddDays(5),
                DataInicio = DateTime.Now.AddDays(6),
                DataFim = DateTime.Now.AddDays(10)
            };
            var eventoFinalizado = new Evento
            {
                Id = 304,
                Nome = "Evento Finalizado",
                Status = "F",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-2),
                DataFimInscricao = DateTime.Now.AddDays(5),
                DataInicio = DateTime.Now.AddDays(6),
                DataFim = DateTime.Now.AddDays(10)
            };

            _context.Eventos.AddRange(eventoAtivo, eventoCadastro, eventoInativo, eventoFinalizado);
            _context.SaveChanges();

            // Act
            var vitrine = _eventoService.GetEventosVitrine("SE").ToList();

            // Assert
            Assert.IsTrue(vitrine.Any(e => e.Id == 301));
            Assert.IsFalse(vitrine.Any(e => e.Id == 302));
            Assert.IsFalse(vitrine.Any(e => e.Id == 303));
            Assert.IsFalse(vitrine.Any(e => e.Id == 304));
        }

        [TestMethod()]
        public void GetEventosVitrine_GaranteExclusaoDeEventosComInscricoesOuEventoVencidos()
        {
            // Arrange
            var eventoValido = new Evento
            {
                Id = 310,
                Nome = "Evento Valido",
                Status = "A",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-2),
                DataFimInscricao = DateTime.Now.AddDays(5),
                DataInicio = DateTime.Now.AddDays(6),
                DataFim = DateTime.Now.AddDays(10)
            };
            var eventoInscricaoVencida = new Evento
            {
                Id = 311,
                Nome = "Evento Inscricao Vencida",
                Status = "A",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-10),
                DataFimInscricao = DateTime.Now.AddDays(-1),
                DataInicio = DateTime.Now.AddDays(2),
                DataFim = DateTime.Now.AddDays(5)
            };
            var eventoJaAconteceu = new Evento
            {
                Id = 312,
                Nome = "Evento Ja Aconteceu",
                Status = "A",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-15),
                DataFimInscricao = DateTime.Now.AddDays(-10),
                DataInicio = DateTime.Now.AddDays(-5),
                DataFim = DateTime.Now.AddDays(-1)
            };

            _context.Eventos.AddRange(eventoValido, eventoInscricaoVencida, eventoJaAconteceu);
            _context.SaveChanges();

            // Act
            var vitrine = _eventoService.GetEventosVitrine("SE").ToList();

            // Assert
            Assert.IsTrue(vitrine.Any(e => e.Id == 310));
            Assert.IsFalse(vitrine.Any(e => e.Id == 311));
            Assert.IsFalse(vitrine.Any(e => e.Id == 312));
        }

        [TestMethod()]
        public void GetEventosVitrine_GaranteInclusaoDeEventosComInscricoesAbertasEEmBreve()
        {
            // Arrange
            var eventoAberto = new Evento
            {
                Id = 320,
                Nome = "Evento Inscricoes Abertas",
                Status = "A",
                Estado = "BA",
                DataInicioInscricao = DateTime.Now.AddDays(-3),
                DataFimInscricao = DateTime.Now.AddDays(4),
                DataInicio = DateTime.Now.AddDays(5),
                DataFim = DateTime.Now.AddDays(10)
            };
            var eventoEmBreve = new Evento
            {
                Id = 321,
                Nome = "Evento Em Breve",
                Status = "A",
                Estado = "BA",
                DataInicioInscricao = DateTime.Now.AddDays(2),
                DataFimInscricao = DateTime.Now.AddDays(10),
                DataInicio = DateTime.Now.AddDays(12),
                DataFim = DateTime.Now.AddDays(15)
            };

            _context.Eventos.AddRange(eventoAberto, eventoEmBreve);
            _context.SaveChanges();

            // Act
            var vitrine = _eventoService.GetEventosVitrine("BA").ToList();

            // Assert
            Assert.IsTrue(vitrine.Any(e => e.Id == 320));
            Assert.IsTrue(vitrine.Any(e => e.Id == 321));
        }

        [TestMethod()]
        public void GetEventosVitrine_GaranteFiltragemPrecisaPorUF()
        {
            // Arrange
            var eventoSE = new Evento
            {
                Id = 330,
                Nome = "Evento Sergipe",
                Status = "A",
                Estado = "SE",
                DataInicioInscricao = DateTime.Now.AddDays(-1),
                DataFimInscricao = DateTime.Now.AddDays(5),
                DataInicio = DateTime.Now.AddDays(6),
                DataFim = DateTime.Now.AddDays(10)
            };
            var eventoSP = new Evento
            {
                Id = 331,
                Nome = "Evento Sao Paulo",
                Status = "A",
                Estado = "SP",
                DataInicioInscricao = DateTime.Now.AddDays(-1),
                DataFimInscricao = DateTime.Now.AddDays(5),
                DataInicio = DateTime.Now.AddDays(6),
                DataFim = DateTime.Now.AddDays(10)
            };

            _context.Eventos.AddRange(eventoSE, eventoSP);
            _context.SaveChanges();

            // Act & Assert
            var vitrineSE = _eventoService.GetEventosVitrine("SE").ToList();
            Assert.IsTrue(vitrineSE.Any(e => e.Id == 330));
            Assert.IsFalse(vitrineSE.Any(e => e.Id == 331));

            var vitrineSP = _eventoService.GetEventosVitrine("SP").ToList();
            Assert.IsTrue(vitrineSP.Any(e => e.Id == 331));
            Assert.IsFalse(vitrineSP.Any(e => e.Id == 330));

            var vitrineTodos = _eventoService.GetEventosVitrine(null).ToList();
            Assert.IsTrue(vitrineTodos.Any(e => e.Id == 330));
            Assert.IsTrue(vitrineTodos.Any(e => e.Id == 331));
        }
    }
}