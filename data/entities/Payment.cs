using System.ComponentModel.DataAnnotations.Schema;

namespace Bakalauras.data.entities
{
    [Table("Payment")]
    public class Payment
    {
        public int Id { get; set; }

        public double Price { get; set; }

        public double Points { get; set; }

        public ICollection<PaymentUser> PaymentUser { get; set; }
    }
}
