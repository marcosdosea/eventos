using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core;
using Core.Service;
using Moq;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Service;

namespace Service.Tests
{
    [TestClass()]
    public class ColaboradorServiceTests
    {
        private Mock<UserManager<UsuarioIdentity>> _mockUserManager;
        private Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private Mock<IPessoaService> _mockPessoaService;
        private IColaboradorService _colaboradorService;

        [TestInitialize]
        public void Initialize()
        {
            // Mocks do UserManager e RoleManager
            _mockUserManager = new Mock<UserManager<UsuarioIdentity>>(
                Mock.Of<IUserStore<UsuarioIdentity>>(), null, null, null, null, null, null, null, null);
            _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(), null, null, null, null);

            // Mock do PessoaService
            _mockPessoaService = new Mock<IPessoaService>();

            // Instância do serviço a ser testado
            _colaboradorService = new ColaboradorService(_mockRoleManager.Object, _mockUserManager.Object, _mockPessoaService.Object);
        }

        [TestMethod()]
        public async Task CreateAsync_WhenPessoaDoesNotExist_CreatesPessoaAndUserAndAddsToRole()
        {
            // Arrange
            var pessoa = GetTestPessoa();
            var usuario = new UsuarioIdentity { UserName = pessoa.Cpf };

            _mockPessoaService.Setup(service => service.GetByCpf(pessoa.Cpf)).Returns((Pessoa)null);
            _mockPessoaService.Setup(service => service.CreateAsync(It.IsAny<Pessoa>())).ReturnsAsync(usuario);
            _mockUserManager.Setup(um => um.FindByNameAsync(pessoa.Cpf)).ReturnsAsync((UsuarioIdentity)null);
            _mockUserManager.Setup(um => um.IsInRoleAsync(usuario, "COLABORADOR")).ReturnsAsync(false);
            _mockUserManager.Setup(um => um.AddToRoleAsync(usuario, "COLABORADOR")).ReturnsAsync(IdentityResult.Success);

            // Act
            await _colaboradorService.CreateAsync(pessoa);

            // Assert
            _mockPessoaService.Verify(service => service.Create(pessoa), Times.Once);
            _mockPessoaService.Verify(service => service.CreateAsync(pessoa), Times.Once);
            _mockUserManager.Verify(um => um.AddToRoleAsync(usuario, "COLABORADOR"), Times.Once);
        }

        [TestMethod()]
        public async Task CreateAsync_WhenUserExists_AddsToRole()
        {
            // Arrange
            var pessoa = GetTestPessoa();
            var usuario = new UsuarioIdentity { UserName = pessoa.Cpf };

            _mockPessoaService.Setup(service => service.GetByCpf(pessoa.Cpf)).Returns(pessoa); // Simula que a pessoa já existe
            _mockUserManager.Setup(um => um.FindByNameAsync(pessoa.Cpf)).ReturnsAsync(usuario);
            _mockUserManager.Setup(um => um.IsInRoleAsync(usuario, "COLABORADOR")).ReturnsAsync(false);
            _mockUserManager.Setup(um => um.AddToRoleAsync(usuario, "COLABORADOR")).ReturnsAsync(IdentityResult.Success);

            // Act
            await _colaboradorService.CreateAsync(pessoa);

            // Assert
            _mockPessoaService.Verify(service => service.Create(It.IsAny<Pessoa>()), Times.Once);
            _mockUserManager.Verify(um => um.AddToRoleAsync(usuario, "COLABORADOR"), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateAsync_WhenPessoaExists_UpdatesPessoaAndUser()
        {
            // Arrange
            var pessoa = GetTestPessoa();
            var existingPessoa = GetTestPessoa();
            existingPessoa.Id = 1; // Simula a pessoa existente no banco de dados
            var usuario = new UsuarioIdentity { UserName = pessoa.Cpf, Email = "old@email.com", PhoneNumber = "old-phone" };

            _mockPessoaService.Setup(service => service.GetByCpf(pessoa.Cpf)).Returns(existingPessoa);
            _mockPessoaService.Setup(service => service.Edit(It.IsAny<Pessoa>()));
            _mockUserManager.Setup(um => um.FindByNameAsync(pessoa.Cpf)).ReturnsAsync(usuario);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<UsuarioIdentity>())).ReturnsAsync(IdentityResult.Success);

            // Act
            await _colaboradorService.UpdateAsync(pessoa);

            // Assert
            _mockPessoaService.Verify(service => service.Edit(It.Is<Pessoa>(p => p.Id == 1)), Times.Once); // Verifica que o ID foi mantido
            _mockUserManager.Verify(um => um.UpdateAsync(It.Is<UsuarioIdentity>(u => u.Email == pessoa.Email && u.PhoneNumber == pessoa.Telefone1)), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateAsync_WhenPessoaDoesNotExist_ThrowsException()
        {
            // Arrange
            var pessoa = GetTestPessoa();
            _mockPessoaService.Setup(service => service.GetByCpf(pessoa.Cpf)).Returns((Pessoa)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(() => _colaboradorService.UpdateAsync(pessoa));
        }

        [TestMethod()]
        public async Task DeleteAsync_WhenColaboradorExists_RemovesFromRoleAndDeletesPessoa()
        {
            // Arrange
            var cpf = "123.456.789-00";
            var pessoa = new Pessoa { Cpf = cpf };
            var usuario = new UsuarioIdentity { UserName = cpf };
            var roles = new List<string> { "COLABORADOR" };

            _mockPessoaService.Setup(service => service.GetByCpf(cpf)).Returns(pessoa);
            _mockUserManager.Setup(um => um.FindByNameAsync(cpf)).ReturnsAsync(usuario);
            _mockUserManager.Setup(um => um.GetRolesAsync(usuario)).ReturnsAsync(roles);
            _mockUserManager.Setup(um => um.RemoveFromRoleAsync(usuario, "COLABORADOR")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.DeleteAsync(usuario)).ReturnsAsync(IdentityResult.Success);
            _mockPessoaService.Setup(service => service.Delete(pessoa.Id));

            // Act
            await _colaboradorService.DeleteAsync(cpf);

            // Assert
            _mockUserManager.Verify(um => um.RemoveFromRoleAsync(usuario, "COLABORADOR"), Times.Once);
            _mockUserManager.Verify(um => um.DeleteAsync(usuario), Times.Once);
            _mockPessoaService.Verify(service => service.Delete(pessoa.Id), Times.Once);
        }

        [TestMethod()]
        public async Task GetColaboradoresAsync_ReturnsColaboradoresInRole()
        {
            // Arrange
            var role = new IdentityRole("COLABORADOR");
            var usersInRole = new List<UsuarioIdentity>
            {
                new UsuarioIdentity { UserName = "111.111.111-11" },
                new UsuarioIdentity { UserName = "222.222.222-22" }
            };

            _mockRoleManager.Setup(rm => rm.FindByNameAsync("COLABORADOR")).ReturnsAsync(role);
            _mockUserManager.Setup(um => um.GetUsersInRoleAsync("COLABORADOR")).ReturnsAsync(usersInRole);
            _mockPessoaService.Setup(service => service.GetByCpf("111.111.111-11")).Returns(new Pessoa { Cpf = "111.111.111-11", Nome = "Pessoa 1", Email = "p1@test.com" });
            _mockPessoaService.Setup(service => service.GetByCpf("222.222.222-22")).Returns(new Pessoa { Cpf = "222.222.222-22", Nome = "Pessoa 2", Email = "p2@test.com" });

            // Act
            var result = await _colaboradorService.GetColaboradoresAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(c => c.Nome == "Pessoa 1"));
            Assert.IsTrue(result.Any(c => c.Nome == "Pessoa 2"));
        }

        private Pessoa GetTestPessoa()
        {
            return new Pessoa
            {
                Nome = "Alexandre Moreira Silva",
                NomeCracha = "Alexandre",
                Cpf = "070.594.845-58",
                Sexo = "M",
                Cep = "48800-000",
                Rua = "Avenida Principal",
                Bairro = "Centro",
                Cidade = "Porto da Folha",
                Estado = "SE",
                Numero = "1503",
                Complemento = "casa",
                Email = "email@gmail.com",
                Telefone1 = "7999990011",
                Telefone2 = null,
            };
        }
    }
}