using Microsoft.AspNetCore.Identity;

namespace ParchePlanAPI.Models;

public class User : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
