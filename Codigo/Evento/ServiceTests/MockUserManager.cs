using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventoWeb.Models;
using Core;

namespace Service.Tests
{
    public class MockUserManager<TUser> : UserManager<TUser> where TUser : class
    {
         private readonly Dictionary<string, TUser> _users = new Dictionary<string, TUser>();

         public MockUserManager()
             : base(new Mock<IUserStore<TUser>>().Object,
                   new Mock<IOptions<IdentityOptions>>().Object,
                   new Mock<IPasswordHasher<TUser>>().Object,
                   new IUserValidator<TUser>[0],
                   new IPasswordValidator<TUser>[0],
                   new Mock<ILookupNormalizer>().Object,
                   new Mock<IdentityErrorDescriber>().Object,
                   new Mock<IServiceProvider>().Object,
                   new Mock<ILogger<UserManager<TUser>>>().Object)
         { }

         public override Task<IdentityResult> CreateAsync(TUser user, string password)
         {
             if (user is UsuarioIdentity usuario)
             {
                 _users[usuario.Id] = user;
                 _users[usuario.Email] = user;
             }
             return Task.FromResult(IdentityResult.Success);
         }

         public override Task<IdentityResult> AddToRoleAsync(TUser user, string role)
         {
             return Task.FromResult(IdentityResult.Success);
         }

         public override Task<TUser> FindByEmailAsync(string email)
         {
             if (_users.TryGetValue(email, out var user))
             {
                 return Task.FromResult(user);
             }
             return Task.FromResult((TUser)null);
         }

         public override Task<TUser> FindByIdAsync(string userId)
         {
             if (_users.TryGetValue(userId, out var user))
             {
                 return Task.FromResult(user);
             }
             return Task.FromResult((TUser)null);
         }

         public override Task<IList<string>> GetRolesAsync(TUser user)
         {
             return Task.FromResult<IList<string>>(new List<string> { "Participante" });
         }
    }
} 