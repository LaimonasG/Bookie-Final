using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;

namespace Bakalauras.data.repositories
{
    public interface IDailyQuestionRepository
    {
        Task CreateAnswers(List<Answer> answers);
        Task<int> CreateQuestion(DailyQuestion question);
        Task DeleteAsync(DailyQuestion question);
        Task<GetQuestionDto?> GetQuestionAsync(string date);
        Task<DailyQuestion?> GetAsync(int id);
        Task<IReadOnlyList<DailyQuestion>> GetManyAsync();
        Task UpdateAsync(DailyQuestion question);
        Task<(bool, object)> AnswerQuestion(int questionId, int answerId, string userId);
        Task<Answer> GetCorrectAsnwer(DailyQuestion question);
        List<Answer> UpdateAnswers(List<Answer> answers, int questionId);
        Task<(bool, object)> WhenWasQuestionAnswered(string userId);
    }
    public class DailyQuestionRepository : IDailyQuestionRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IProfileRepository _ProfileRepository;
        public DailyQuestionRepository(BookieDBContext context,IProfileRepository repp)
        {
            _BookieDBContext = context;
            _ProfileRepository = repp;
        }

        public async Task<GetQuestionDto?> GetQuestionAsync(string DateString)
        {
            var questions = await GetManyAsync();
            DateTime date = DateTime.Parse(DateString);
            DailyQuestion rez = questions.Where(x => x.Date.Year == date.Year && x.Date.Month == date.Month && x.Date.Day == date.Day).FirstOrDefault();
            if (rez == null) { return null; }
            GetQuestionDto result = new GetQuestionDto
            (
                rez.Id,
                rez.Question,
                rez.Points,
                rez.Date,
                new List<Answer>()
            ) ;
            var answers = await _BookieDBContext.Answers.Where(x => x.QuestionId == rez.Id).ToListAsync();
            result.Answers.AddRange(answers);

            
            return result;
        }

        public async Task<DailyQuestion?> GetAsync(int id)
        {
            return  await _BookieDBContext.DailyQuestions.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IReadOnlyList<DailyQuestion>> GetManyAsync()
        {
            return await _BookieDBContext.DailyQuestions.ToListAsync();
        }

        public async Task<int> CreateQuestion(DailyQuestion question)
        {
            _BookieDBContext.DailyQuestions.Add(question);
            await _BookieDBContext.SaveChangesAsync();
            return question.Id;
        }

        public async Task CreateAnswers(List<Answer> answers)
        {
            _BookieDBContext.Answers.AddRange(answers);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(DailyQuestion question)
        {
            _BookieDBContext.DailyQuestions.Update(question);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(DailyQuestion question)
        {
            _BookieDBContext.DailyQuestions.Remove(question);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<(bool, object)> AnswerQuestion(int questionId, int answerId, string userId)
        {
            var question = await GetAsync(questionId);
            if (question == null) { return (false, "Question not found"); }
            var trueAnswer = await GetCorrectAsnwer(question);
            if (trueAnswer == null) { return (false, "Correct answer not found"); }
            var userProfile = await _ProfileRepository.GetAsync(userId);
            if (userProfile == null) { return (false, "User profile not found"); }
            var answer = await _BookieDBContext.Answers.FirstOrDefaultAsync(x => x.Id == answerId);
            if (answer == null) { return (false, "Answer not found"); }

            DailyQuestionProfile dqp = new DailyQuestionProfile { DailyQuestionId = question.Id, ProfileId = userProfile.Id };

            var existingDqp = await _BookieDBContext.DailyQuestionProfiles.FirstOrDefaultAsync(x => x.DailyQuestionId == dqp.DailyQuestionId && x.ProfileId == dqp.ProfileId);
            if (existingDqp != null) { return (false, "Question already answered"); }

            if (answer == trueAnswer)
            {
                userProfile.Points += question.Points;
                dqp.IsCorrect = true;
            }
            else { dqp.IsCorrect = false; }

            dqp.DateAnswered = DateTime.Now;
            _BookieDBContext.DailyQuestionProfiles.Add(dqp);
            _BookieDBContext.Profiles.Update(userProfile);

            await _BookieDBContext.SaveChangesAsync();
            AnswerDto result = new AnswerDto(trueAnswer.Content, dqp.IsCorrect ? 1 : 0);
            return (true, result);
        }

        public async Task<Answer> GetCorrectAsnwer(DailyQuestion question)
        {
            return await _BookieDBContext.Answers.FirstOrDefaultAsync(x => x.QuestionId == question.Id && x.Correct==1);
        }

        public List<Answer> UpdateAnswers(List<Answer> answers, int questionId)
        {
            List<Answer> rez=new List<Answer>();
            foreach (var ans in answers)
            {
                rez.Add(new Answer
                {
                    Content = ans.Content,
                    QuestionId = questionId,
                    Correct = ans.Correct
                });
            }
            return rez;
        }

        public async Task<(bool, object)> WhenWasQuestionAnswered(string userId)
        {
            Profile profile=await _ProfileRepository.GetAsync(userId);
            var dqp = await _BookieDBContext.DailyQuestionProfiles
                      .OrderByDescending(x => x.DateAnswered)
                      .FirstOrDefaultAsync(x => x.ProfileId == profile.Id);

            if (dqp != null) { return (true, dqp.DateAnswered); }
            return (false, "Question wasn't answered yet");
        }
    }
}
