namespace Bakalauras.data.entities
{
    public class DailyQuestion
    {
        public int Id { get; set; }

        public string Question { get; set; }

        public double Points { get; set; }

        public DateTime Date { get; set; }

        public ICollection<DailyQuestionProfile> DailyQuestionProfiles { get; set; }

    }
}
