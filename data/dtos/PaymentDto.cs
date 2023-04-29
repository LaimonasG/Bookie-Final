namespace Bakalauras.data.dtos
{
   public record PaymentCreateDto(double Points,double Price);
    public record PaymentDto(int Id,double Points, double Price);
}
