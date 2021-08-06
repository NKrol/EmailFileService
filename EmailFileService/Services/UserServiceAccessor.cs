using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Services
{
    public interface IUserServiceAccessor
    {
        ClaimsPrincipal User { get; }
        string GetMainDirectory { get; }
        public int? GetId { get; }
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

        public string GetMainDirectory =>
            _httpContextAccessor.HttpContext?.User.FindFirst(c => c.Type == "MainDirectory")?.Value;
    }
}
