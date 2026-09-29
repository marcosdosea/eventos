using Microsoft.AspNetCore.Identity;

namespace EventoWeb
{
    /// <summary>
    /// Traduz as mensagens padrão do Identity (ex. erros de senha e de usuário
    /// duplicado exibidos no cadastro) para o português.
    /// </summary>
    public class IdentityErrorDescriberPtBr : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            new() { Code = nameof(DefaultError), Description = "Ocorreu um erro desconhecido." };

        public override IdentityError ConcurrencyFailure() =>
            new() { Code = nameof(ConcurrencyFailure), Description = "Falha de concorrência otimista. O objeto foi modificado por outro processo." };

        public override IdentityError PasswordMismatch() =>
            new() { Code = nameof(PasswordMismatch), Description = "Senha incorreta." };

        public override IdentityError InvalidToken() =>
            new() { Code = nameof(InvalidToken), Description = "Token inválido." };

        public override IdentityError RecoveryCodeRedemptionFailed() =>
            new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Falha ao validar o código de recuperação." };

        public override IdentityError LoginAlreadyAssociated() =>
            new() { Code = nameof(LoginAlreadyAssociated), Description = "Já existe um usuário com este login." };

        public override IdentityError InvalidUserName(string? userName) =>
            new() { Code = nameof(InvalidUserName), Description = $"O nome de usuário '{userName}' é inválido. São permitidos apenas letras, números e os caracteres -._@+." };

        public override IdentityError InvalidEmail(string? email) =>
            new() { Code = nameof(InvalidEmail), Description = $"O e-mail '{email}' é inválido." };

        public override IdentityError DuplicateUserName(string? userName) =>
            new() { Code = nameof(DuplicateUserName), Description = $"O nome de usuário '{userName}' já está em uso." };

        public override IdentityError DuplicateEmail(string? email) =>
            new() { Code = nameof(DuplicateEmail), Description = $"O e-mail '{email}' já está em uso." };

        public override IdentityError InvalidRoleName(string? role) =>
            new() { Code = nameof(InvalidRoleName), Description = $"O perfil '{role}' é inválido." };

        public override IdentityError DuplicateRoleName(string? role) =>
            new() { Code = nameof(DuplicateRoleName), Description = $"O perfil '{role}' já existe." };

        public override IdentityError UserAlreadyHasPassword() =>
            new() { Code = nameof(UserAlreadyHasPassword), Description = "O usuário já possui uma senha cadastrada." };

        public override IdentityError UserLockoutNotEnabled() =>
            new() { Code = nameof(UserLockoutNotEnabled), Description = "O bloqueio não está habilitado para este usuário." };

        public override IdentityError UserAlreadyInRole(string? role) =>
            new() { Code = nameof(UserAlreadyInRole), Description = $"O usuário já pertence ao perfil '{role}'." };

        public override IdentityError UserNotInRole(string? role) =>
            new() { Code = nameof(UserNotInRole), Description = $"O usuário não pertence ao perfil '{role}'." };

        public override IdentityError PasswordTooShort(int length) =>
            new() { Code = nameof(PasswordTooShort), Description = $"A senha deve ter ao menos {length} caracteres." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"A senha deve conter ao menos {uniqueChars} caracteres diferentes." };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "A senha deve conter ao menos um caractere especial." };

        public override IdentityError PasswordRequiresDigit() =>
            new() { Code = nameof(PasswordRequiresDigit), Description = "A senha deve conter ao menos um dígito ('0'-'9')." };

        public override IdentityError PasswordRequiresLower() =>
            new() { Code = nameof(PasswordRequiresLower), Description = "A senha deve conter ao menos uma letra minúscula ('a'-'z')." };

        public override IdentityError PasswordRequiresUpper() =>
            new() { Code = nameof(PasswordRequiresUpper), Description = "A senha deve conter ao menos uma letra maiúscula ('A'-'Z')." };
    }
}
