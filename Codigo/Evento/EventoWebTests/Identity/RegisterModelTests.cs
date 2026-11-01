using Core;
using Core.Service;
using EventoWeb.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventoWeb.Identity.Tests
{
    [TestClass()]
    public class RegisterModelTests
    {
        private Mock<UserManager<UsuarioIdentity>> _userManagerMock;
        private Mock<IUserStore<UsuarioIdentity>> _userStoreMock;
        private Mock<SignInManager<UsuarioIdentity>> _signInManagerMock;
        private Mock<ILogger<RegisterModel>> _loggerMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<IPessoaService> _pessoaServiceMock;
        private RegisterModel _model;

        [TestInitialize()]
        public void Initialize()
        {
            _userStoreMock = new Mock<IUserStore<UsuarioIdentity>>();
            _userManagerMock = new Mock<UserManager<UsuarioIdentity>>(
                _userStoreMock.Object, null, null, null, null, null, null, null, null);

            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<UsuarioIdentity>>();
            _signInManagerMock = new Mock<SignInManager<UsuarioIdentity>>(
                _userManagerMock.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                null, null, null, null);

            _signInManagerMock
                .Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(new List<AuthenticationScheme>());

            _loggerMock = new Mock<ILogger<RegisterModel>>();
            _emailServiceMock = new Mock<IEmailService>();
            _pessoaServiceMock = new Mock<IPessoaService>();

            _model = new RegisterModel(
                _userManagerMock.Object,
                _userStoreMock.Object,
                _signInManagerMock.Object,
                _loggerMock.Object,
                _emailServiceMock.Object,
                _pessoaServiceMock.Object)
            {
                PageContext = new PageContext
                {
                    ActionDescriptor = new CompiledPageActionDescriptor(),
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData()
                },
                Url = new Mock<IUrlHelper>().Object,
                Input = new RegisterModel.InputModel
                {
                    Nome = "Usuário Teste",
                    CPF = "000.000.000-00",
                    Email = "teste@exemplo.com",
                    Password = "Password123!",
                    ConfirmPassword = "Password123!"
                }
            };
        }

        [TestMethod()]
        public async Task OnPostAsync_WhenIdentityReturnsDuplicateEmail_AttachesFriendlyErrorToEmailField()
        {
            _pessoaServiceMock
                .Setup(p => p.GetByCpf(It.IsAny<string>()))
                .Returns((Pessoa)null);

            _pessoaServiceMock
                .Setup(p => p.ValidaEmail(It.IsAny<string>()))
                .Returns(true);

            _pessoaServiceMock
                .Setup(p => p.EmailExist(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var duplicateError = new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "O e-mail 'teste@exemplo.com' já está em uso."
            };

            _userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<UsuarioIdentity>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(duplicateError));

            var result = await _model.OnPostAsync();

            Assert.IsInstanceOfType(result, typeof(PageResult));
            Assert.IsFalse(_model.ModelState.IsValid);
            Assert.IsTrue(_model.ModelState.ContainsKey("Input.Email"));
            Assert.AreEqual("O e-mail 'teste@exemplo.com' já está em uso.", _model.ModelState["Input.Email"].Errors[0].ErrorMessage);
        }
    }
}
