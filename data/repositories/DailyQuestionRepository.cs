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
        Task<DailyQuestion?> GetRandomAsync();
        Task<DailyQuestion?> GetAsync(int id);
        Task<IReadOnlyList<DailyQuestion>> GetManyAsync();
        Task UpdateAsync(DailyQuestion question);
        Task<Answer> AnswerQuestion(int questionId, int answerId, string userId);
        Task<Answer> GetCorrectAsnwer(DailyQuestion question);
        List<Answer> UpdateAnswers(List<AnswerDto> answers, int questionId);
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

        public async Task<DailyQuestion?> GetRandomAsync()
        {
            var questions=await GetManyAsync();
            Random random = new Random();
            int randomIndex = random.Next(questions.Count);
            DailyQuestion randomItem = questions[randomIndex];
            return randomItem;
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

        public async Task<Answer> AnswerQuestion(int questionId, int answerId, string userId)
        {
            var question = await GetAsync(questionId);
            if (question == null) { return null; }
            var trueAnswer = await GetCorrectAsnwer(question);
            if (trueAnswer == null) { return null; }
            var userProfile = await _ProfileRepository.GetAsync(userId);
            if (userProfile == null) { return null; }
            var answer= await _BookieDBContext.Answers.FirstOrDefaultAsync(x => x.Id == answerId);
            if (answer == null) { return null; }

            DailyQuestionProfile dqp=new DailyQuestionProfile { DailyQuestionId= question.Id,ProfileId=userProfile.Id };
            if (answer == trueAnswer)
            {
                userProfile.Points += question.Points;
                dqp.IsCorrect = true;
            }
            else { dqp.IsCorrect = false; }

            _BookieDBContext.DailyQuestionProfiles.Add(dqp);
            _BookieDBContext.Profiles.Update(userProfile);

            await _BookieDBContext.SaveChangesAsync();

            return trueAnswer;
        }

        public async Task<Answer> GetCorrectAsnwer(DailyQuestion question)
        {
            return await _BookieDBContext.Answers.FirstOrDefaultAsync(x => x.QuestionId == question.Id && x.Correct==1);
        }

        public List<Answer> UpdateAnswers(List<AnswerDto> answers, int questionId)
        {
            List<Answer> rez=new List<Answer>();
            foreach (var ans in answers)
            {
                rez.Add(new Answer
                {
                    Content = ans.content,
                    QuestionId = questionId,
                    Correct = ans.correct
                });
            }
            return rez;
        }
    }
}
