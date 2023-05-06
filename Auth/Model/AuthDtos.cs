using System.ComponentModel.DataAnnotations;

namespace Bakalauras.Auth
{
    public record RegisterUserDto([Required] string UserName, [EmailAddress][Required] string Email, [Required] string Password);

    public record LoginDto(string UserName, string Password);

    public record UserDto(string Id, string UserName, string Email);

    public record UserBlockedDto(string Id, string UserName,int isBlocked);

    public record SuccessfulLoginDto(string AccessToken);

    public record RefreshTokenDto(string AccessToken);
}
