namespace Bakalauras.data.entities
{
    public class DailyQuestionProfile
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        public int DailyQuestionId { get; set; }
        public DailyQuestion Book { get; set; }
    }
}
