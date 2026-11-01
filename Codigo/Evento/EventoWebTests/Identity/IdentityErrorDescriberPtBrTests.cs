using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventoWeb.Identity.Tests
{
    [TestClass()]
    public class IdentityErrorDescriberPtBrTests
    {
        private IdentityErrorDescriberPtBr _describer;

        [TestInitialize()]
        public void Initialize()
        {
            _describer = new IdentityErrorDescriberPtBr();
        }

        [TestMethod()]
        public void DuplicateEmail_ReturnsFriendlyMessageInPortuguese()
        {
            const string email = "teste@exemplo.com";

            var error = _describer.DuplicateEmail(email);

            Assert.IsNotNull(error);
            Assert.AreEqual("DuplicateEmail", error.Code);
            Assert.AreEqual($"O e-mail '{email}' já está em uso.", error.Description);
        }

        [TestMethod()]
        public void DuplicateUserName_ReturnsFriendlyMessageInPortuguese()
        {
            const string userName = "usuario.teste";

            var error = _describer.DuplicateUserName(userName);

            Assert.IsNotNull(error);
            Assert.AreEqual("DuplicateUserName", error.Code);
            Assert.AreEqual($"O nome de usuário '{userName}' já está em uso.", error.Description);
        }

        [TestMethod()]
        public void InvalidEmail_ReturnsFriendlyMessageInPortuguese()
        {
            const string email = "email_invalido";

            var error = _describer.InvalidEmail(email);

            Assert.IsNotNull(error);
            Assert.AreEqual("InvalidEmail", error.Code);
            Assert.AreEqual($"O e-mail '{email}' é inválido.", error.Description);
        }

        [TestMethod()]
        public void PasswordTooShort_ReturnsFriendlyMessageInPortuguese()
        {
            const int length = 6;

            var error = _describer.PasswordTooShort(length);

            Assert.IsNotNull(error);
            Assert.AreEqual("PasswordTooShort", error.Code);
            Assert.AreEqual($"A senha deve ter ao menos {length} caracteres.", error.Description);
        }
    }
}
