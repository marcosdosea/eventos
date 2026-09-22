using Microsoft.AspNetCore.Identity;

namespace Util
{
    public class SemEspacoSenhaValidator<TUser> : IPasswordValidator<TUser>
        where TUser : class
    {
        public Task<IdentityResult> ValidateAsync(
            UserManager<TUser> manager,
            TUser user,
            string password)
        {
            if (password.Any(char.IsWhiteSpace))
            {
                return Task.FromResult(
                    IdentityResult.Failed(
                        new IdentityError
                        {
                            Description = "A senha não pode conter espaços."
                        }));
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}