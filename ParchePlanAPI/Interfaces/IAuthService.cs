using Microsoft.AspNetCore.Identity;

namespace ParchePlanAPI.Interfaces;

public interface IAuthService
{ 
    public Task<IdentityResult> Register(string email, string password, string fullName, string major, string? avatarUrl);
    Task<string> Login(string email, string password);
}