using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bakalauras.data.entities
{
    public class PaymentUser
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        public int PaymentId { get; set; }
        public Payment Payment { get; set; }

        public DateTime Date { get; set; }
    }
}
