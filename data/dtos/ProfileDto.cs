using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record PersonalInfoDto(string userId, string? userName,string? email);
    public record CreateProfileDto(string userId);
    public record UpdateProfileDto(string userId, double points);
    public record ProfileDto(string userId, string userName, string email, double points, byte[] profilePicture);

    public record ProfileBooksDto(string userId, string userName, List<Book> books);
}
