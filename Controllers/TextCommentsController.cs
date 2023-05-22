using Bakalauras.Auth;
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
    [Route("api/genres/{genreName}/texts/{textId}/comments")]
    public class TextCommentsController : ControllerBase
    {
        private readonly ICommentRepository _CommentRepository;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly ITextRepository _TextRepository;
        private const string _Type = "Text";
        public TextCommentsController(ICommentRepository repo, IAuthorizationService authService,
            UserManager<BookieUser> userManager, ITextRepository textRepository)
        {
            _CommentRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
            _TextRepository = textRepository;
        }
        [HttpGet]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetMany(int textId)
        {
            var comments = await _CommentRepository.GetManyAsync(textId, _Type);

            if (comments.Count == 0)
            {
                return new List<CommentDto>();
            }
            return Ok(comments.Select(x => new CommentDto(x.Id, x.EntityId, _Type, x.Date, x.Content, x.UserId, x.Username)));
        }

        [HttpGet]
        [Route("{commentId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<CommentDto>> Get(int textId, int commentId)
        {
            var comment = await _CommentRepository.GetAsync(commentId, textId, _Type);
            if (comment == null) return NotFound();
            return new CommentDto(comment.Id, comment.EntityId, _Type, DateTime.Now, comment.Content,
                comment.UserId, comment.Username);
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<CommentDto>> Create(CreateCommentDto createCommentDto, int textId)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user.isBlocked)
            {
                return BadRequest("Komentavimas uždraustas, naudotojas užblokuotas");
            }

            if (createCommentDto.Content == null && createCommentDto.Content == "")
            {
                return BadRequest("Komentaro tekstas negali būti tuščias.");
            }
            var comment = new Comment
            {
                EntityType = _Type,
                Content = createCommentDto.Content,
                Date = DateTime.Now,
                Username = user.UserName,
                UserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            };

            var text = await _TextRepository.GetAsync(textId);
            if (text == null) return NotFound();

            await _CommentRepository.CreateAsync(comment, textId, _Type);

            //201
            return Created("201", new CommentDto(comment.Id, comment.EntityId, _Type, comment.Date, comment.Content,
                comment.UserId, comment.Username));
        }

        [HttpPut]
        [Route("{commentId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<CommentDto>> Update(int commentId, int textId, UpdateCommentDto updateCommentDto)
        {
            var comment = await _CommentRepository.GetAsync(commentId, textId, _Type);
            if (comment == null) return NotFound();
            var authRez = await _AuthorizationService.AuthorizeAsync(User, comment, PolicyNames.ResourceOwner);
            if (!authRez.Succeeded)
            {
                return Forbid();
            }
            comment.Content = updateCommentDto.Content;
            await _CommentRepository.UpdateAsync(comment);

            //200
            return Ok(new CommentDto(comment.Id, comment.EntityId, _Type, DateTime.Now, comment.Content, comment.UserId, comment.Username));
        }

        [HttpDelete]
        [Route("{commentId}")]
        public async Task<ActionResult> Remove(int commentId, int textId)
        {
            var comment = await _CommentRepository.GetAsync(commentId, textId, _Type);
            if (comment == null) return NotFound();
            var authRez = await _AuthorizationService.AuthorizeAsync(User, comment, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }
            await _CommentRepository.DeleteAsync(comment);

            //204
            return NoContent();
        }
    }
}
