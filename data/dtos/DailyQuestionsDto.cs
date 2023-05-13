using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record CreateQuestionDto(string Question, double Points, DateTime DateToRelease, List<Answer> Answers);
    public record GetQuestionDto(int Id, string Question, double Points, DateTime DateToRelease, List<Answer> Answers);
    public record DailyQuestionDto(string Question, double points, List<Answer> answers);
    public record AnswerDto(string content, int correct);
    public record GetQuestionDtoParam(string Date);
    public record AnswerQuestionDto(int QuestionID, int AnswerID);

}
