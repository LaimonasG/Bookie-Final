namespace Bakalauras.data.entities
{
    public class DailyQuestionProfile
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int DailyQuestionId { get; set; }
        public DailyQuestion DailyQuestion { get; set; }

        public bool IsCorrect { get; set; }
        public DateTime DateAnswered { get; set; }
    }
}
