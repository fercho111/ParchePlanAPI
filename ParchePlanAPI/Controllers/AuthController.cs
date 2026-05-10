using ParchePlanAPI.Interfaces;
using ParchePlanAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ParchePlanAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// test pwd: 45ngOw0u.3
public class AuthController : Controller
{
    // GET
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        this._authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO model)
    {
        var result = await _authService.Register(model.Email, model.Password, model.FullName, model.Major, model.AvatarUrl);
        if (result.Succeeded)
        {
            return Ok(new { message = $"Usuario {model.Email} creado con éxito" });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO login)
    {
        var token = await _authService.Login(login.Email, login.Password);
        if (token != null)
        {
            return Ok(new { Token = token });
        }

        return Unauthorized(new { Message = "Credenciales incorrectas" });
    }
    
    public IActionResult Index()
    {
        return View();
    }
}