using System.Security.Claims;

namespace HealthMate.Application.Abstractions.Identity.Ports;

public interface IJwtTokenIssuer
{
    string Issue(IReadOnlyList<Claim> claims, bool rememberMe);
}
