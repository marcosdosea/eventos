using Service;
using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;

namespace Service.Tests
{
    [TestClass()]
    public class PessoaServiceTests
    {

        private EventoContext _context;
        private IPessoaService _pessoaService;

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
            var pessoas = new List<Pessoa>
                {
                new Pessoa
                {
                        Id = 1,
                        Nome = "João Vitor Sodré",
                        NomeCracha = "Sodré",
                        Cpf = "12244667",
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
                        Cpf = "12345678",
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
                        Cpf = "12244667",
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

            _context.AddRange(pessoas);
            _context.SaveChanges();

            _pessoaService = new PessoaService(_context);
        }

        [TestMethod()]
        public void CreateTest()
        {
            // Act
            _pessoaService.Create(new Pessoa()
            {
                Id = 4,
                Nome = "Marcos Venicios da Palma Dias",
                NomeCracha = "Marcos Venicios",
                Cpf = "12244667",
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
            });
            // Assert
            Assert.AreEqual(4, _pessoaService.GetAll().Count());
            var pessoa = _pessoaService.Get(4);
            Assert.AreEqual("Marcos Venicios da Palma Dias", pessoa.Nome);
            Assert.AreEqual("Marcos Venicios", pessoa.NomeCracha);
            Assert.AreEqual("12244667", pessoa.Cpf);
            Assert.AreEqual("M", pessoa.Sexo);
            Assert.AreEqual("45340086", pessoa.Cep);
            Assert.AreEqual("Rua da Linha", pessoa.Rua);
            Assert.AreEqual("Centro", pessoa.Bairro);
            Assert.AreEqual("Esplanada", pessoa.Cidade);
            Assert.AreEqual("BA", pessoa.Estado);
            Assert.AreEqual("s/n", pessoa.Numero);
            Assert.AreEqual("casa", pessoa.Complemento);
            Assert.AreEqual("muzanpvp@gmail.com", pessoa.Email);
            Assert.AreEqual("7999900113344", pessoa.Telefone1);
            Assert.AreEqual("NULL", pessoa.Telefone2);
        }

        [TestMethod()]
        public void DeleteTest()
        {
            // Act
            _pessoaService.Delete(1);
            // Assert
            Assert.AreEqual(2, _pessoaService.GetAll().Count());
            var areainteresse = _pessoaService.Get(1);
            Assert.AreEqual(null, areainteresse);
        }

        [TestMethod()]
        public void EditTest()
        {
            //Act 
            var pessoa = _pessoaService.Get(3);
            pessoa.Id = 3;
            pessoa.Nome = "Marcos Venicios da Palma Dias";
            pessoa.NomeCracha = "Marcos Venicios";
            pessoa.Cpf = "12244667";
            pessoa.Sexo = "M";
            pessoa.Cep = "45340086";
            pessoa.Rua = "Rua da Linha";
            pessoa.Bairro = "Centro";
            pessoa.Cidade = "Esplanada";
            pessoa.Estado = "BA";
            pessoa.Numero = "s/n";
            pessoa.Complemento = "casa";
            pessoa.Email = "muzanpvp@gmail.com";
            pessoa.Telefone1 = "7999900113344";
            pessoa.Telefone2 = "NULL";
            //Assert
            pessoa = _pessoaService.Get(3);
            Assert.AreEqual((uint)3, pessoa.Id);
            Assert.AreEqual("Marcos Venicios da Palma Dias", pessoa.Nome);
            Assert.AreEqual("Marcos Venicios", pessoa.NomeCracha);
            Assert.AreEqual("12244667", pessoa.Cpf);
            Assert.AreEqual("M", pessoa.Sexo);
            Assert.AreEqual("45340086", pessoa.Cep);
            Assert.AreEqual("Rua da Linha", pessoa.Rua);
            Assert.AreEqual("Centro", pessoa.Bairro);
            Assert.AreEqual("Esplanada", pessoa.Cidade);
            Assert.AreEqual("BA", pessoa.Estado);
            Assert.AreEqual("s/n", pessoa.Numero);
            Assert.AreEqual("casa", pessoa.Complemento);
            Assert.AreEqual("muzanpvp@gmail.com", pessoa.Email);
            Assert.AreEqual("7999900113344", pessoa.Telefone1);
            Assert.AreEqual("NULL", pessoa.Telefone2);
        }

        [TestMethod()]
        public void GetTest()
        {
            var pessoa = _pessoaService.Get(2);
            Assert.IsNotNull(_pessoaService);
            Assert.AreEqual((uint)2, pessoa.Id);
            Assert.AreEqual("Nagibe Santos Wanus Junior", pessoa.Nome);
            Assert.AreEqual("Nagibe Junior", pessoa.NomeCracha);
            Assert.AreEqual("12345678", pessoa.Cpf);
            Assert.AreEqual("M", pessoa.Sexo);
            Assert.AreEqual("45566000", pessoa.Cep);
            Assert.AreEqual("Rua Severino Vieira", pessoa.Rua);
            Assert.AreEqual("Centro", pessoa.Bairro);
            Assert.AreEqual("Esplanada", pessoa.Cidade);
            Assert.AreEqual("BA", pessoa.Estado);
            Assert.AreEqual("147", pessoa.Numero);
            Assert.AreEqual("casa", pessoa.Complemento);
            Assert.AreEqual("nagibejr@gmail.com", pessoa.Email);
            Assert.AreEqual("75999643467", pessoa.Telefone1);
            Assert.AreEqual("NULL", pessoa.Telefone2);
        }

        [TestMethod()]
        public void GetAllTest()
        {
            // Act
            var listaPessoa = _pessoaService.GetAll();
            // Assert
            Assert.IsInstanceOfType(listaPessoa, typeof(IEnumerable<Pessoa>));
            Assert.IsNotNull(listaPessoa);
            Assert.AreEqual(3, listaPessoa.Count());
            var firstPessoa = listaPessoa.First();
            Assert.AreEqual((uint)1, firstPessoa.Id);
        }
    }
}