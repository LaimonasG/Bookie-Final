namespace Bakalauras.data.dtos
{
    public record WriterSalesData(List<BookSalesData> bookData, List<TextSalesData> textData);

    public record BookSalesData(string BookName,string Genre, double BookPrice, int SalesAmount, List<DateTime?> BoughtDates,
        int CurrSubscribedAmount);
    public record TextSalesData(string TextName,string Genre, double TextPrice, int SalesAmount, List<DateTime?> BoughtDate);

    public record CashOutDto(double cashOutPercent);

    public record WriterPaymentConfirmation(bool Confirmed, bool WithrawalTooSmall, double PointAmount, double EurAmount);
}

