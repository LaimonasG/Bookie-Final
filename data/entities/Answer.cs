namespace Bakalauras.data.entities
{
    public class Answer
    {
        public int Id { get; set; }

        public string Content { get; set; }

        public int Correct { get; set; }

        public int QuestionId { get; set; }
    }
}
