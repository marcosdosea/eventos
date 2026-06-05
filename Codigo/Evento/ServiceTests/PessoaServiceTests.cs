using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using EventoWeb.Models;
using System;
using System.Collections.Immutable;
using Service;

namespace Service.Tests
{
    [TestClass()]
    public class PessoaServiceTests
    {
        private EventoContext _context;
        private IPessoaService _pessoaService;
        private MockUserManager<UsuarioIdentity> _userManager;
        private IInscricaoService _inscricaoService;

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
                    Cpf = "040.268.930-57",
                    Sexo = "M",
                    Cep = "48370-000",
                    Rua = "Avenida Principal",
                    Bairro = "Centro",
                    Cidade = "Irece",
                    Estado = "BA",
                    Numero = "s/n",
                    Complemento = "casa",
                    Email = "email@gmail.com",
                    Telefone1 = "7999990011",
                    Telefone2 = "NULL",
                },
                new Pessoa
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
                },
                new Pessoa
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
                },
            };

            var evento = new Evento
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

            var papel = new Papel
            {
                Id = 1,
                Nome = "Gestor",
            };

            _context.AddRange(pessoas);
            _context.AddRange(evento);
            _context.AddRange(papel);
            _context.SaveChanges();

            _userManager = new MockUserManager<UsuarioIdentity>();
            _inscricaoService = new InscricaoService(_context, _userManager);
            _pessoaService = new PessoaService(_userManager, _context, _inscricaoService);
        }

        [TestMethod()]
        public void CreateTest()
        {
            _pessoaService.Create(new Pessoa()
            {
                Id = 4,
                Nome = "Marcos Venicios da Palma Dias",
                NomeCracha = "Marcos Venicios",
                Cpf = "206.065.300-04",
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
            });

            Assert.AreEqual(4, _pessoaService.GetAll().Count());
            var pessoa = _pessoaService.Get(4);
            Assert.AreEqual("Marcos Venicios da Palma Dias", pessoa.Nome);
            Assert.AreEqual("Marcos Venicios", pessoa.NomeCracha);
            Assert.AreEqual("206.065.300-04", pessoa.Cpf);
            Assert.AreEqual("M", pessoa.Sexo);
            Assert.AreEqual("45340-086", pessoa.Cep);
            Assert.AreEqual("Rua da Linha", pessoa.Rua);
            Assert.AreEqual("Centro", pessoa.Bairro);
            Assert.AreEqual("Esplanada", pessoa.Cidade);
            Assert.AreEqual("BA", pessoa.Estado);
            Assert.AreEqual("s/n", pessoa.Numero);
            Assert.AreEqual("casa", pessoa.Complemento);
            Assert.AreEqual("muzanpvp@gmail.com", pessoa.Email);
            Assert.AreEqual("7999001133", pessoa.Telefone1);
            Assert.AreEqual("NULL", pessoa.Telefone2);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            _pessoaService.Delete(1);

            Assert.AreEqual(2, _pessoaService.GetAll().Count());
            var areainteresse = _pessoaService.Get(1);
            Assert.AreEqual(null, areainteresse);
        }

        [TestMethod()]
        public async Task EditTest()
        {
            
            var pessoa = _pessoaService.Get(3);

            
            pessoa.Nome = "Marcos Venicios da Palma Dias Alterado";
            pessoa.NomeCracha = "Marcos Alterado";
            pessoa.Cpf = "206.015.300-04";
            pessoa.Sexo = "M";
            pessoa.Cep = "45340-086";
            pessoa.Rua = "Rua da Linha";
            pessoa.Bairro = "Centro";
            pessoa.Cidade = "Esplanada";
            pessoa.Estado = "BA";
            pessoa.Numero = "100"; 
            pessoa.Complemento = "casa";
            pessoa.Email = "muzanpvp_alterado@gmail.com"; 
            pessoa.Telefone1 = "7999001133";
            pessoa.Telefone2 = "NULL";

            
            await _pessoaService.Edit(pessoa);

            
            var pessoaVerificada = _pessoaService.Get(3);

            
            Assert.AreEqual((uint)3, pessoaVerificada.Id);
            Assert.AreEqual("Marcos Venicios da Palma Dias Alterado", pessoaVerificada.Nome);
            Assert.AreEqual("Marcos Alterado", pessoaVerificada.NomeCracha);
            Assert.AreEqual("206.015.300-04", pessoaVerificada.Cpf);
            Assert.AreEqual("M", pessoaVerificada.Sexo);
            Assert.AreEqual("45340-086", pessoaVerificada.Cep);
            Assert.AreEqual("Rua da Linha", pessoaVerificada.Rua);
            Assert.AreEqual("Centro", pessoaVerificada.Bairro);
            Assert.AreEqual("Esplanada", pessoaVerificada.Cidade);
            Assert.AreEqual("BA", pessoaVerificada.Estado);
            Assert.AreEqual("100", pessoaVerificada.Numero);
            Assert.AreEqual("casa", pessoaVerificada.Complemento);
            Assert.AreEqual("muzanpvp_alterado@gmail.com", pessoaVerificada.Email);
            Assert.AreEqual("7999001133", pessoaVerificada.Telefone1);
            Assert.AreEqual("NULL", pessoaVerificada.Telefone2);
        }

        [TestMethod()]
        public void GetTest()
        {
            var pessoa = _pessoaService.Get(2);
            Assert.IsNotNull(_pessoaService);
            Assert.AreEqual((uint)2, pessoa.Id);
            Assert.AreEqual("Nagibe Santos Wanus Junior", pessoa.Nome);
            Assert.AreEqual("Nagibe Junior", pessoa.NomeCracha);
            Assert.AreEqual("917.091.250-55", pessoa.Cpf);
            Assert.AreEqual("M", pessoa.Sexo);
            Assert.AreEqual("45566-000", pessoa.Cep);
            Assert.AreEqual("Rua Severino Vieira", pessoa.Rua);
            Assert.AreEqual("Centro", pessoa.Bairro);
            Assert.AreEqual("Esplanada", pessoa.Cidade);
            Assert.AreEqual("BA", pessoa.Estado);
            Assert.AreEqual("147", pessoa.Numero);
            Assert.AreEqual("casa", pessoa.Complemento);
            Assert.AreEqual("nagibejr@gmail.com", pessoa.Email);
            Assert.AreEqual("7599643467", pessoa.Telefone1);
            Assert.AreEqual("NULL", pessoa.Telefone2);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            var listaPessoa = _pessoaService.GetAll();

            Assert.IsInstanceOfType(listaPessoa, typeof(IEnumerable<Pessoa>));
            Assert.IsNotNull(listaPessoa);
            Assert.AreEqual(3, listaPessoa.Count());
            var firstPessoa = listaPessoa.First();
            Assert.AreEqual((uint)1, firstPessoa.Id);
        }

        /*
        Não estava passando, pois a arquitetura de testes atual 
        não suporta as transações.
        */

        //[TestMethod()]
        //public async Task CreateGestorModelTest()
        //{
        //    var pessoa = _pessoaService.Get(1);
        //    await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 2);
        //    var usuario = await _userManager.FindByNameAsync(pessoa.Cpf);
        //    Assert.IsNotNull(usuario);
        //    Assert.IsTrue(await _userManager.IsInRoleAsync(usuario, "GESTOR"));
        //}

        //[TestMethod()]
        //public async Task CreateAdministradorAsync()
        //{
        //    var pessoa = new Pessoa
        //    {
        //        Id = 4,
        //        Nome = "Sinéad O'Connor Null Nullberg",
        //        NomeCracha = "Sineád O'Connor",
        //        Cpf = "883.069.820-29",
        //        Email = "sine_connor.9@academico.ufs.br",
        //        Telefone1 = "7999990011"
        //    };
        //    await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 1);
        //    var usuario = await _userManager.FindByNameAsync(pessoa.Cpf);
        //    Assert.IsNotNull(usuario);
        //    Assert.IsTrue(await _userManager.IsInRoleAsync(usuario, "ADMINISTRADOR"));
        //    Assert.AreEqual(pessoa.Email, usuario.Email);
        //    Assert.AreEqual(pessoa.Telefone1, usuario.PhoneNumber);
        //}
    }
}