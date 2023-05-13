using Bakalauras.data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bakalauras.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(string username, string userId, IEnumerable<string> roles);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly SymmetricSecurityKey _AuthSigningKey;
    private readonly string _Issuer;
    private readonly string _Audience;
    public JwtTokenService(IConfiguration configuration)
    {
        _AuthSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));
        _Issuer = configuration["JWT:ValidIssuer"];
        _Audience = configuration["JWT:ValidAudience"];
    }

    public string CreateAccessToken(string username, string userId, IEnumerable<string> roles)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
        };

        authClaims.AddRange(roles.Select(userRole => new Claim(ClaimTypes.Role, userRole)));

        var accessSecurityToken = new JwtSecurityToken
        (
            issuer: _Issuer,
            audience: _Audience,
            expires: DateTime.UtcNow.AddHours(1),
            claims: authClaims,
            signingCredentials: new SigningCredentials(_AuthSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(accessSecurityToken);
    }
}