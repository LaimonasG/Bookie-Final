using Bakalauras.Auth.Model;
using Bakalauras.Auth;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/profiles/dailyQuestion")]
    public class DailyQuestionController : ControllerBase
    {
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IDailyQuestionRepository _DailyQuestionRepository;

        public DailyQuestionController(UserManager<BookieUser> userMng,IDailyQuestionRepository repd)
        {
            _DailyQuestionRepository = repd;
            _UserManager = userMng;
        }
        //[HttpGet]
        //[Route("/all")]
        //[Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        //public async Task<IEnumerable<DailyQuestion>> GetMany()
        //{
        //    return await _DailyQuestionRepository.GetManyAsync();
        //}

        [HttpGet]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<DailyQuestion>> Get()
        {
            var question = await _DailyQuestionRepository.GetRandomAsync();
            if (question == null) return NotFound();
            return question;
        }

        //[HttpPost]
        //public async Task<DailyQuestionDto> Create(CreateQuestionDto dto)
        //{
        //    DailyQuestion q = new DailyQuestion { Question = dto.Question, Points = dto.Points };
        //    int questionId= await _DailyQuestionRepository.CreateQuestion(q);
        //    var answers = _DailyQuestionRepository.UpdateAnswers(dto.Answers,questionId);

        //   await _DailyQuestionRepository.CreateAnswers(answers);

        //    return new DailyQuestionDto(q.Question, q.Points, answers);
        //}

        [HttpPut]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<AnswerDto>> AnswerQuestion(AnswerQuestionDto dto)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            AnswerDto result = await _DailyQuestionRepository.AnswerQuestion(dto.QuestionID,dto.AnswerID, user.Id);

            return Ok(result);
        }
    }
}
