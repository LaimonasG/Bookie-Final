using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record PersonalInfoDto(string? Username, string? Email, string? Name, string? Surname);

    public record AuthorInfoDto(string? Name, string? Surname);
    public record CreateProfileDto(string userId);
    public record UpdateProfilePointsDto(int Points);
    public record ProfileDto(string userId, string Name, string Surname, string userName, string email, double points);
    public record ProfileBooksDto(string userId, string userName, double Points, List<SubscribeToBookDto> books);

    public record ProfileTextsDto(string userId, string userName, List<Text> texts);

    public record ProfileBookOffersDto(int bookId, List<int> MissingChapters);

    public record ProfilePayDto(int bookId, int chapterId);
}
