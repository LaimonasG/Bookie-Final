using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Bakalauras.Auth.Model;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using Bakalauras.data;
using Microsoft.EntityFrameworkCore;
//using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Bakalauras.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(string username, string userId, IEnumerable<string> roles);
    string CreateRefreshToken();
    Task UpdateRefreshTokenAsync(BookieUser user, string oldRefreshToken, string newRefreshToken);
    Task<BookieUser> GetUserByRefreshTokenAsync(string refreshToken);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly SymmetricSecurityKey _AuthSigningKey;
    private readonly string _Issuer;
    private readonly string _Audience;
    private readonly BookieDBContext _Context;
    public JwtTokenService(IConfiguration configuration, BookieDBContext cont)
    {
        _AuthSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));
        _Issuer = configuration["JWT:ValidIssuer"];
        _Audience = configuration["JWT:ValidAudience"];
        _Context = cont;
    }

    public string CreateAccessToken(string username,string userId,IEnumerable<string> roles)
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
        ) ;

        return new JwtSecurityTokenHandler().WriteToken(accessSecurityToken);
    }

    public string CreateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public async Task<BookieUser> GetUserByRefreshTokenAsync(string refreshToken)
    {
        return await _Context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }

    public async Task UpdateRefreshTokenAsync(BookieUser user, string oldRefreshToken, string newRefreshToken)
    {
        if (user.RefreshToken != oldRefreshToken)
            throw new InvalidOperationException("The provided refresh token does not match the user's refresh token.");

        user.RefreshToken = newRefreshToken;
        _Context.Update(user);
        await _Context.SaveChangesAsync();
    }
}