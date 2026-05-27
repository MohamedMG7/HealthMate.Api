using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HealthMate.Application.Abstractions.Identity.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HealthMate.Infrastructure.Identity.Adapters;

public sealed class JwtTokenIssuer(IConfiguration configuration) : IJwtTokenIssuer
{
    public string Issue(IReadOnlyList<Claim> claims, bool rememberMe)
    {
        var secret = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Jwt:Key is not configured.");
        }

        var signingCredential = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            signingCredentials: signingCredential,
            expires: rememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(2));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
