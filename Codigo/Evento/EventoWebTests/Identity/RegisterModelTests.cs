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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
                Input = new RegisterModel.InputModel
                {
                    Nome = "Usuário Teste",
                    CPF = "529.982.247-25",
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

        [TestMethod()]
        [DataRow("000.000.000-00")]
        [DataRow("111.111.111-11")]
        [DataRow("888.888.888-88")]
        [DataRow("999.999.999-99")]
        [DataRow("123.456.789-00")]
        public async Task OnPostAsync_ComCpfInvalido_RejeitaSemCriarUsuario(string cpfInvalido)
        {
            _model.Input.CPF = cpfInvalido;

            var result = await _model.OnPostAsync();

            Assert.IsInstanceOfType(result, typeof(PageResult));
            Assert.IsFalse(_model.ModelState.IsValid);
            Assert.IsTrue(_model.ModelState.ContainsKey("Input.CPF"));
            Assert.IsFalse(new Util.CPFAttribute().IsValid(cpfInvalido));
            Assert.IsFalse(Util.Methods.ValidarCpf(cpfInvalido));
            _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<UsuarioIdentity>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod()]
        public async Task OnPostAsync_RequireConfirmedAccount_RedirecionaSemExporCpfNaQueryString()
        {
            _userManagerMock.Object.Options = new IdentityOptions();
            _userManagerMock.Object.Options.SignIn.RequireConfirmedAccount = true;

            var urlMock = new Mock<IUrlHelper>();
            urlMock.Setup(u => u.ActionContext).Returns(_model.PageContext);
            urlMock.Setup(u => u.Content(It.IsAny<string>())).Returns("/");
            urlMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://localhost/Identity/Account/ConfirmEmail");
            _model.Url = urlMock.Object;

            _pessoaServiceMock.Setup(p => p.GetByCpf(It.IsAny<string>())).Returns((Pessoa)null);
            _pessoaServiceMock.Setup(p => p.ValidaEmail(It.IsAny<string>())).Returns(true);
            _pessoaServiceMock.Setup(p => p.EmailExist(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _pessoaServiceMock.Setup(p => p.Create(It.IsAny<Pessoa>())).Returns(10u);
            _pessoaServiceMock.Setup(p => p.Get(10u)).Returns(new Pessoa { Id = 10, Nome = "Usuário Teste", Cpf = "52998224725", Email = "teste@exemplo.com" });
            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<UsuarioIdentity>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<UsuarioIdentity>(), "USUARIO")).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<UsuarioIdentity>())).ReturnsAsync("token-123");
            _emailServiceMock.Setup(e => e.ModeloConfirmEmail(It.IsAny<string>(), It.IsAny<Pessoa>(), It.IsAny<string>())).ReturnsAsync(true);

            var result = await _model.OnPostAsync();

            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
            var redirect = (RedirectToPageResult)result;
            Assert.AreEqual("RegisterConfirmation", redirect.PageName);
            Assert.IsNotNull(redirect.RouteValues);
            Assert.AreEqual("teste@exemplo.com", redirect.RouteValues["email"]);
            Assert.IsFalse(redirect.RouteValues.ContainsKey("cpf"), "CPF não deve ser exposto na query string.");
            Assert.AreEqual("52998224725", _model.TempData["Cpf"]);
        }
    }
}
