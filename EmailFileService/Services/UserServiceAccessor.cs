using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Services
{
    public interface IUserServiceAccessor
    {
        ClaimsPrincipal User { get; }
        public int? GetId { get; }
        string? GetEmail { get; }
    }

    public class UserServiceAccessor : IUserServiceAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserServiceAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public int? GetId =>
            User is null ? null : (int?) int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value);

        public string? GetEmail =>
            User?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Email)?.Value;
    }
}
