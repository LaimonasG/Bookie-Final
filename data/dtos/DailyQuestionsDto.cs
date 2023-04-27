using Bakalauras.data.entities;
using System.ComponentModel.DataAnnotations;

namespace Bakalauras.data.dtos
{
    public record  CreateQuestionDto(string Question, double Points,DateTime DateToRelease, List<Answer> Answers);
    public record DailyQuestionDto(string Question, double points, List<Answer> answers);

    public record AnswerDto(string content, int correct);


  //  public record AnswerQuestionDto(int QuestionID, int AnswerID);

    public class AnswerQuestionDto
    {
        [Required(ErrorMessage = "QuestionID is required", AllowEmptyStrings = false)]
        public int QuestionID { get;  set; }
        [Required(ErrorMessage = "AnswerID is required", AllowEmptyStrings = false)]
        public int AnswerID { get;  set; }

    }
}
