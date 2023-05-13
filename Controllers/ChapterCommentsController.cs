using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/genres/{genreName}/books/{bookId}/chapters/{chapterId}/comments")]
    public class ChapterCommentsController : ControllerBase
    {
        private readonly ICommentRepository _CommentRepository;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IChaptersRepository _ChaptersRepository;
        private const string _Type = "Chapter";
        public ChapterCommentsController(ICommentRepository repo,
            UserManager<BookieUser> userManager, IChaptersRepository chaptersRepository)
        {
            _CommentRepository = repo;
            _UserManager = userManager;
            _ChaptersRepository = chaptersRepository;
        }
        [HttpGet]
        public async Task<IEnumerable<CommentDto>> GetMany(int chapterId)
        {
            var comments = await _CommentRepository.GetManyAsync(chapterId, _Type);
            return comments.Select(x => new CommentDto(x.Id, x.EntityId, _Type, x.Date, x.Content, x.UserId, x.Username));
        }

        [HttpGet]
        [Route("{commentId}")]
        public async Task<ActionResult<CommentDto>> Get(int chapterId, int commentId)
        {
            var comment = await _CommentRepository.GetAsync(commentId, chapterId, _Type);
            if (comment == null) return NotFound();
            return new CommentDto(comment.Id, comment.EntityId, _Type, DateTime.Now, comment.Content,
                comment.UserId, comment.Username);
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<CommentDto>> Create(CreateCommentDto createCommentDto, int chapterId, int bookId)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user.isBlocked)
            {
                return BadRequest("Komentavimas uždraustas, naudotojas užblokuotas");
            }
            var comment = new Comment
            {
                EntityType = _Type,
                Content = createCommentDto.Content,
                Date = DateTime.Now,
                Username = user.UserName,
                UserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            };

            var chapter = await _ChaptersRepository.GetAsync(chapterId, bookId);
            if (chapter == null) return NotFound();

            await _CommentRepository.CreateAsync(comment, chapterId, _Type);

            return Created("201", new CommentDto(comment.Id, comment.EntityId, _Type, comment.Date, comment.Content,
                comment.UserId, comment.Username));
        }
    }
}
