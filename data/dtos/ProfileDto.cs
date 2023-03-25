using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record PersonalInfoDto(string userId, string? userName,string? email);
    public record CreateProfileDto(string userId);
    public record UpdateProfileDto(string userId, double points);
    public record ProfileDto(string userId, string userName, string email, double points);

    public record ProfileBooksDto(string userId, string userName, List<Book> books);

    public record ProfileTextsDto(string userId, string userName, List<Text> texts);

    public record ProfileBookOffersDto(int bookId,int periodDays,int PeriodAmount,double price);

    public record ProfilePurchacesDto(List<Tuple<int, DateTime>> BookPayments, List<Tuple<int, DateTime>> TextPayments);

    public record ProfileBookPaymentDto(int bookId, double paymentAmount);
}
