using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventoWeb.Models;
using Core;

namespace Service.Tests
{
    public class MockUserManager<TUser> : UserManager<TUser> where TUser : class
    {
        private readonly Dictionary<string, TUser> _users = new();
        private readonly Dictionary<string, List<string>> _userRoles = new();

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
                if (!string.IsNullOrEmpty(usuario.UserName))
                    _users[usuario.UserName] = user;

                if (!string.IsNullOrEmpty(usuario.Email))
                    _users[usuario.Email] = user;

                if (!string.IsNullOrEmpty(usuario.Id))
                    _users[usuario.Id] = user;
                
                _userRoles[usuario.UserName] = new List<string>();
            }
            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<TUser> FindByNameAsync(string userName)
        {
            if (_users.TryGetValue(userName, out var user))
            {
                return Task.FromResult(user);
            }
            return Task.FromResult<TUser>(null);
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

        public override Task<IdentityResult> AddToRoleAsync(TUser user, string role)
        {
            if (user is UsuarioIdentity usuario)
            {
                if (!_userRoles.ContainsKey(usuario.UserName))
                    _userRoles[usuario.UserName] = new List<string>();
                
                if (!_userRoles[usuario.UserName].Contains(role))
                    _userRoles[usuario.UserName].Add(role);
            }
            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<IdentityResult> RemoveFromRoleAsync(TUser user, string role)
        {
            if (user is UsuarioIdentity usuario && _userRoles.ContainsKey(usuario.UserName))
            {
                _userRoles[usuario.UserName].Remove(role);
            }
            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<bool> IsInRoleAsync(TUser user, string role)
        {
            if (user is UsuarioIdentity usuario && _userRoles.ContainsKey(usuario.UserName))
            {
                return Task.FromResult(_userRoles[usuario.UserName].Contains(role));
            }
            return Task.FromResult(false);
        }

        public override Task<IList<string>> GetRolesAsync(TUser user)
        {
            if (user is UsuarioIdentity usuario && _userRoles.ContainsKey(usuario.UserName))
            {
                return Task.FromResult<IList<string>>(_userRoles[usuario.UserName].ToList());
            }
            return Task.FromResult<IList<string>>(new List<string>());
        }
    }
}