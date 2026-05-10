using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ParchePlanAPI.Interfaces;
using ParchePlanAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace ParchePlanAPI.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        this._userManager = userManager;
        this._roleManager = roleManager;
        this._configuration = configuration;
    }

    public async Task<IdentityResult> Register(string email, string password, string fullName, string major, string? avatarUrl)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Major = major,
            AvatarUrl = avatarUrl
        };
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<string> Login(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && await _userManager.CheckPasswordAsync(user, password))
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            return GetJwtToken(user, userRoles);
        }
        return null;
    }

    private string GetJwtToken(User user, IList<string> roles)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var authSignatureKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(
                authSignatureKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
 