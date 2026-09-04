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
            builder.UseInMemoryDatabase("Evento")
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
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

        [TestMethod()]
        public async Task GetAllAdmAsync_ReturnsOnlyAdmins()
        {
            var pessoaAdm = new Pessoa
            {
                Id = 4,
                Nome = "Admin Teste",
                NomeCracha = "Admin",
                Cpf = "111.111.111-11",
                Sexo = "M",
                Email = "admin@teste.com",
                Telefone1 = "7999999999"
            };
            _context.Pessoas.Add(pessoaAdm);
            await _context.SaveChangesAsync();

            var usuarioAdm = new UsuarioIdentity
            {
                UserName = pessoaAdm.Cpf,
                Email = pessoaAdm.Email,
                PhoneNumber = pessoaAdm.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuarioAdm, "Temp@1234!");
            await _userManager.AddToRoleAsync(usuarioAdm, "ADMINISTRADOR");

            // Pessoa sem vínculo Identity não deve aparecer na lista de admins
            var pessoaSemUsuario = new Pessoa
            {
                Id = 5,
                Nome = "Sem Usuario",
                NomeCracha = "SemUsuario",
                Cpf = "555.555.555-55",
                Sexo = "M",
                Email = "semusuario@teste.com",
                Telefone1 = "7999999998"
            };
            _context.Pessoas.Add(pessoaSemUsuario);

            // Pessoa com usuário mas sem papel de admin não deve aparecer
            var pessoaComum = _pessoaService.Get(1);
            var usuarioComum = new UsuarioIdentity
            {
                UserName = pessoaComum.Cpf,
                Email = pessoaComum.Email,
                PhoneNumber = pessoaComum.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuarioComum, "Temp@1234!");
            await _userManager.AddToRoleAsync(usuarioComum, "USUARIO");
            await _context.SaveChangesAsync();

            var admins = await _pessoaService.GetAllAdmAsync();

            Assert.IsNotNull(admins);
            Assert.AreEqual(1, admins.Count);
            Assert.AreEqual((uint)4, admins[0].Id);
            Assert.AreEqual("Admin Teste", admins[0].Nome);
            Assert.AreEqual("Admin", admins[0].NomeCracha);
            Assert.AreEqual(pessoaAdm.Cpf, admins[0].Cpf);
            Assert.AreEqual("admin@teste.com", admins[0].Email);
            Assert.AreEqual("7999999999", admins[0].Telefone1);
            Assert.IsFalse(admins.Any(a => a.Cpf == pessoaComum.Cpf));
            Assert.IsFalse(admins.Any(a => a.Cpf == pessoaSemUsuario.Cpf));
        }

        [TestMethod()]
        public async Task GetAllAdmAsync_ReturnsEmpty_WhenNoAdmins()
        {
            var admins = await _pessoaService.GetAllAdmAsync();

            Assert.IsNotNull(admins);
            Assert.AreEqual(0, admins.Count);
        }

        [TestMethod()]
        public async Task IsAdmAsync_ReturnsTrue_WhenUserIsAdmin()
        {
            var pessoa = _pessoaService.Get(1);
            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");
            await _userManager.AddToRoleAsync(usuario, "ADMINISTRADOR");

            var isAdm = await _pessoaService.IsAdmAsync(pessoa);

            Assert.IsTrue(isAdm);
        }

        [TestMethod()]
        public async Task IsAdmAsync_ReturnsFalse_WhenUserIsNotAdmin()
        {
            var pessoa = _pessoaService.Get(1);
            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");
            await _userManager.AddToRoleAsync(usuario, "USUARIO");

            var isAdm = await _pessoaService.IsAdmAsync(pessoa);

            Assert.IsFalse(isAdm);
        }

        [TestMethod()]
        public async Task IsAdmAsync_ReturnsFalse_WhenUserNotFound()
        {
            var pessoa = new Pessoa
            {
                Id = 999,
                Nome = "Não Existe",
                Cpf = "999.999.999-99",
                Email = "naoexiste@teste.com"
            };

            var isAdm = await _pessoaService.IsAdmAsync(pessoa);

            Assert.IsFalse(isAdm);
        }

        [TestMethod()]
        public async Task CreateAdministradorAsync_CreatesAdminWithRole()
        {
            var pessoa = new Pessoa
            {
                Id = 4,
                Nome = "Novo Admin",
                NomeCracha = "Admin",
                Cpf = "111.111.111-11",
                Sexo = "M",
                Cep = "48370-000",
                Rua = "Rua Teste",
                Bairro = "Centro",
                Cidade = "Irece",
                Estado = "BA",
                Numero = "100",
                Complemento = "casa",
                Email = "novo@admin.com",
                Telefone1 = "7999999999",
                Telefone2 = "NULL"
            };

            var sucesso = await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 0, 1);

            Assert.IsTrue(sucesso);
            var usuario = await _userManager.FindByNameAsync(pessoa.Cpf);
            Assert.IsNotNull(usuario);
            Assert.IsTrue(await _userManager.IsInRoleAsync(usuario, "ADMINISTRADOR"));
            Assert.AreEqual(pessoa.Cpf, usuario.UserName);
            Assert.AreEqual(pessoa.Email, usuario.Email);
            Assert.AreEqual(pessoa.Telefone1, usuario.PhoneNumber);
            Assert.IsTrue(usuario.EmailConfirmed);

            // Pessoa deve estar persistida no contexto
            var pessoaPersistida = _pessoaService.GetByCpf(pessoa.Cpf);
            Assert.IsNotNull(pessoaPersistida);
            Assert.AreEqual("Novo Admin", pessoaPersistida.Nome);
            Assert.AreEqual("novo@admin.com", pessoaPersistida.Email);
            Assert.AreEqual(4, _pessoaService.GetAll().Count());
        }

        [TestMethod()]
        public async Task CreateAdministradorAsync_ReturnsFalse_WhenAdminAlreadyExists()
        {
            var pessoaExistente = _pessoaService.Get(1);
            var usuario = new UsuarioIdentity
            {
                UserName = pessoaExistente.Cpf,
                Email = pessoaExistente.Email,
                PhoneNumber = pessoaExistente.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");
            await _userManager.AddToRoleAsync(usuario, "ADMINISTRADOR");

            var pessoaNova = new Pessoa
            {
                Id = 4,
                Nome = "Outro Admin",
                NomeCracha = "Admin2",
                Cpf = pessoaExistente.Cpf,
                Sexo = "M",
                Email = "outro@admin.com",
                Telefone1 = "7999999999"
            };

            var sucesso = await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoaNova, 0, 1);

            Assert.IsFalse(sucesso);
            // Nenhuma pessoa duplicada deve ser criada
            Assert.AreEqual(3, _pessoaService.GetAll().Count());
            var usuarioMantido = await _userManager.FindByNameAsync(pessoaExistente.Cpf);
            Assert.IsNotNull(usuarioMantido);
            Assert.IsTrue(await _userManager.IsInRoleAsync(usuarioMantido, "ADMINISTRADOR"));
        }

        [TestMethod()]
        public async Task CreatePessoaIdentityComPapelAsync_CreatesGestor_WhenIdPapelIs2()
        {
            var pessoa = new Pessoa
            {
                Id = 4,
                Nome = "Novo Gestor",
                NomeCracha = "Gestor",
                Cpf = "222.222.222-22",
                Sexo = "M",
                Email = "gestor@teste.com",
                Telefone1 = "7999999999"
            };
            _pessoaService.Create(pessoa);

            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");

            var sucesso = await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 0, 2);

            Assert.IsTrue(sucesso);
            var usuarioResult = await _userManager.FindByNameAsync(pessoa.Cpf);
            Assert.IsNotNull(usuarioResult);
            Assert.IsTrue(await _userManager.IsInRoleAsync(usuarioResult, "GESTOR"));
        }

        [TestMethod()]
        public async Task CreatePessoaIdentityComPapelAsync_CreatesUsuario_WhenIdPapelIs4()
        {
            var pessoa = new Pessoa
            {
                Id = 4,
                Nome = "Novo Usuario",
                NomeCracha = "Usuario",
                Cpf = "333.333.333-33",
                Sexo = "M",
                Email = "usuario@teste.com",
                Telefone1 = "7999999999"
            };
            _pessoaService.Create(pessoa);

            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");

            var sucesso = await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 0, 4);

            Assert.IsTrue(sucesso);
            var usuarioResult = await _userManager.FindByNameAsync(pessoa.Cpf);
            Assert.IsNotNull(usuarioResult);
            Assert.IsTrue(await _userManager.IsInRoleAsync(usuarioResult, "USUARIO"));
            Assert.IsFalse(await _userManager.IsInRoleAsync(usuarioResult, "ADMINISTRADOR"));
            Assert.IsNotNull(_pessoaService.GetByCpf(pessoa.Cpf));
        }

        [TestMethod()]
        public async Task CreatePessoaIdentityComPapelAsync_Throws_WhenPapelInvalido()
        {
            var pessoa = new Pessoa
            {
                Id = 4,
                Nome = "Papel Invalido",
                NomeCracha = "Invalido",
                Cpf = "444.444.444-44",
                Sexo = "M",
                Email = "invalido@teste.com",
                Telefone1 = "7999999999"
            };
            _pessoaService.Create(pessoa);
            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");

            await Assert.ThrowsExceptionAsync<Exception>(() =>
                _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 0, 99));
        }

        [TestMethod()]
        public void ValidaEmail_ReturnsTrue_ForValidEmail()
        {
            Assert.IsTrue(_pessoaService.ValidaEmail("admin@teste.com"));
            Assert.IsTrue(_pessoaService.ValidaEmail("nome.sobrenome@dominio.com.br"));
        }

        [TestMethod()]
        public void ValidaEmail_ReturnsFalse_WhenDomainHasNoDot()
        {
            // MailAddress aceita, mas o domínio sem ponto é rejeitado pelo regex
            Assert.IsFalse(_pessoaService.ValidaEmail("teste@example"));
        }

        [TestMethod()]
        public async Task EmailExist_ReturnsFalse_WhenEmailNotRegistered()
        {
            Assert.IsFalse(await _pessoaService.EmailExist("ninguem@teste.com", "000.000.000-00"));
        }

        [TestMethod()]
        public async Task EmailExist_ReturnsFalse_WhenEmailBelongsToSameCpf()
        {
            var pessoa = _pessoaService.Get(1);
            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");

            Assert.IsFalse(await _pessoaService.EmailExist(pessoa.Email, pessoa.Cpf));
        }

        [TestMethod()]
        public async Task EmailExist_ReturnsTrue_WhenEmailBelongsToOtherCpf()
        {
            var pessoa = _pessoaService.Get(1);
            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");

            Assert.IsTrue(await _pessoaService.EmailExist(pessoa.Email, "000.000.000-00"));
        }

        [TestMethod()]
        public async Task VerificaEdit_ReturnsFalse_WhenPessoaIsNull()
        {
            Assert.IsFalse(await _pessoaService.VerificaEdit(null));
        }

        [TestMethod()]
        public async Task VerificaEdit_ReturnsFalse_WhenCpfNotFound()
        {
            var pessoa = new Pessoa
            {
                Id = 99,
                Nome = "Fantasma",
                Cpf = "000.000.000-00",
                Email = "fantasma@teste.com",
                Telefone1 = "7999999999"
            };

            Assert.IsFalse(await _pessoaService.VerificaEdit(pessoa));
        }

        [TestMethod()]
        public async Task VerificaEdit_ReturnsTrue_WithoutChanges()
        {
            var pessoaAtual = _pessoaService.Get(1);
            var pessoaIgual = new Pessoa
            {
                Id = pessoaAtual.Id,
                Nome = pessoaAtual.Nome,
                NomeCracha = pessoaAtual.NomeCracha,
                Cpf = pessoaAtual.Cpf,
                Sexo = pessoaAtual.Sexo,
                Email = pessoaAtual.Email,
                Telefone1 = pessoaAtual.Telefone1,
                Telefone2 = pessoaAtual.Telefone2
            };

            var resultado = await _pessoaService.VerificaEdit(pessoaIgual);

            Assert.IsTrue(resultado);
            var mantida = _pessoaService.Get(1);
            Assert.AreEqual(pessoaAtual.Nome, mantida.Nome);
            Assert.AreEqual(pessoaAtual.Email, mantida.Email);
        }

        [TestMethod()]
        public async Task VerificaEdit_Updates_WhenDataChanges()
        {
            var pessoaAtualizada = new Pessoa
            {
                Nome = "João Vitor Atualizado",
                NomeCracha = "João",
                Cpf = "040.268.930-57",
                Email = "novoemail@teste.com",
                Telefone1 = "7999990000",
                Telefone2 = "NULL"
            };

            var resultado = await _pessoaService.VerificaEdit(pessoaAtualizada);

            Assert.IsTrue(resultado);
            var verificada = _pessoaService.Get(1);
            Assert.AreEqual((uint)1, verificada.Id);
            Assert.AreEqual("João Vitor Atualizado", verificada.Nome);
            Assert.AreEqual("novoemail@teste.com", verificada.Email);
            Assert.AreEqual("7999990000", verificada.Telefone1);
            // Sexo é preservado do registro atual
            Assert.AreEqual("M", verificada.Sexo);
        }

        [TestMethod()]
        public async Task Delete_ReturnsFalse_ForLastAdministrator()
        {
            var pessoa = _pessoaService.Get(1);
            var usuario = new UsuarioIdentity
            {
                UserName = pessoa.Cpf,
                Email = pessoa.Email,
                PhoneNumber = pessoa.Telefone1,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(usuario, "Temp@1234!");
            await _userManager.AddToRoleAsync(usuario, "ADMINISTRADOR");

            var sucesso = await _pessoaService.Delete(1);

            Assert.IsFalse(sucesso);
            // Último admin não pode ser removido: pessoa permanece no contexto
            Assert.IsNotNull(_pessoaService.Get(1));
            Assert.AreEqual(3, _pessoaService.GetAll().Count());
            Assert.IsTrue(await _userManager.IsInRoleAsync(usuario, "ADMINISTRADOR"));
        }

        [TestMethod()]
        public async Task Delete_RemovesAdminRole_WhenOtherAdminExists()
        {
            foreach (var pessoaBase in _pessoaService.GetAll().Take(2).ToList())
            {
                var usuarioBase = new UsuarioIdentity
                {
                    UserName = pessoaBase.Cpf,
                    Email = pessoaBase.Email,
                    PhoneNumber = pessoaBase.Telefone1,
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(usuarioBase, "Temp@1234!");
                await _userManager.AddToRoleAsync(usuarioBase, "ADMINISTRADOR");
            }

            var pessoa = _pessoaService.Get(1);
            var sucesso = await _pessoaService.Delete(1);

            Assert.IsTrue(sucesso);
            var usuario = await _userManager.FindByNameAsync(pessoa.Cpf);
            Assert.IsNotNull(usuario);
            Assert.IsFalse(await _userManager.IsInRoleAsync(usuario, "ADMINISTRADOR"));
            // Sem outros papéis, o usuário é rebaixado para USUARIO e a pessoa é mantida
            Assert.IsTrue(await _userManager.IsInRoleAsync(usuario, "USUARIO"));
            Assert.IsNotNull(_pessoaService.Get(1));
        }

        [TestMethod()]
        public async Task Delete_RemovesCommonPerson()
        {
            var sucesso = await _pessoaService.Delete(2);

            Assert.IsTrue(sucesso);
            Assert.IsNull(_pessoaService.Get(2));
            Assert.AreEqual(2, _pessoaService.GetAll().Count());
        }

        [TestMethod()]
        public async Task Delete_ReturnsFalse_WhenPersonNotFound()
        {
            Assert.IsFalse(await _pessoaService.Delete(999));
        }
    }
}