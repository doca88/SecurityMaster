using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SecurityMasterService.Controllers;

public record LoginRequest(string Username, string Password);

[ApiController]
[Route("auth")]
[AllowAnonymous]                 // login mora da radi bez tokena
public class AuthController : ControllerBase
{
    private readonly IConfiguration config;

    public AuthController(IConfiguration config)
    {
        this.config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // za sada jedan zakucan korisnik; kasnije provera u bazi
        if (request.Username != "admin" || request.Password != "admin")
            return Unauthorized("Wrong username or password.");

        var jwt = this.config.GetSection("Jwt");
        var expiresAt = DateTime.UtcNow.AddHours(8);   // jedan radni dan

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.Role, "Admin"),
            }),
            Expires = expiresAt,
            Issuer = jwt["Issuer"],
            Audience = jwt["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
                SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return Ok(new { token, username = request.Username, expiresAt });
    }
}