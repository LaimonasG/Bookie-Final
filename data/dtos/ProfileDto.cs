using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record PersonalInfoDto(string? userName,string? email);
    public record CreateProfileDto(string userId);
    public record UpdateProfilePointsDto(int Points);
    public record ProfileDto(string userId, string userName, string email, double points);
    public record ProfileBooksDto(string userId, string userName,double Points, List<SubscribeToBookDto> books);

    public record ProfileTextsDto(string userId, string userName, List<Text> texts);

    public record ProfileBookOffersDto(int bookId,List<int> MissingChapters);

    public record ProfilePayDto(int bookId);
    public record ProfilePurchacesDto(List<Tuple<int, int>> BookPayments, List<Tuple<int, int>> TextPayments);

    public record ProfileBookPaymentDto(int bookId, double paymentAmount);

    public record ProfileWriterSalesData(List<BookSalesData> bookData, List<TextSalesData> textData);

    public record BookSalesData(string BookName, double BookPrice, int SalesAmount, List<DateTime?> BoughtDates,
        int CurrSubscribedAmount);

    public record TextSalesData(string TextName, double TextPrice, int SalesAmount, List<DateTime?> BoughtDate);

}
