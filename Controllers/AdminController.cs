using Bakalauras.Auth.Model;
using Bakalauras.data.repositories;
using Bakalauras.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bakalauras.data.dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Bakalauras.Auth;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController:ControllerBase
    {
        private readonly IAdminRepository _AdminRepository;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly RoleManager<IdentityRole> _RoleManager;
        private readonly IProfileRepository _ProfileRepository;
        private readonly BookieDBContext _BookieDBContext;
        private readonly ICommentRepository _CommentRepository;
        private readonly IBookRepository _BookRepository;
        private readonly ITextRepository _TextRepository;
        public AdminController(UserManager<BookieUser> mng,IAdminRepository adrep, RoleManager<IdentityRole> roleManager,
            IProfileRepository repp, BookieDBContext dbc, ICommentRepository commentRepository, IBookRepository bookRepository,
            ITextRepository textRepository)
        {
            _UserManager = mng;
            _AdminRepository = adrep;
            _RoleManager = roleManager;
            _ProfileRepository = repp;
            _BookieDBContext = dbc;
            _CommentRepository = commentRepository;
            _BookRepository = bookRepository;
            _TextRepository= textRepository;
        }

        [HttpGet]
        [Route("users")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult<List<UserAdminPageDto>>> GetUsers()
        {
            var users = await _AdminRepository.GetUserList();
            return users;
        }

        [HttpPut]
        [Route("block")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult> UpdateUserBlockedStatus(UserBlockedDto dto)
        {
            var user = await _UserManager.FindByIdAsync(dto.Id);

            if (user == null)
                return BadRequest("Vartotojas nerastas.");

            if (dto.isBlocked == 0)
                user.isBlocked = true;
            else if (dto.isBlocked == 1)
                user.isBlocked = false;
            else
                return BadRequest("Duomenys netinkami.");

            await _UserManager.UpdateAsync(user);

            return Ok(new UserBlockedDto(user.Id, user.UserName, user.isBlocked ? 1 :0));
        }

        [HttpPut]
        [Route("points")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult> UpdateUserPoints(UserAdminPageDto dto)
        {
            var user = await _UserManager.FindByIdAsync(dto.id);

            if (user == null)
                return BadRequest("Vartotojas nerastas.");

            var profile = await _ProfileRepository.GetAsync(user.Id);
            profile.Points = dto.points;

            _BookieDBContext.Profiles.Update(profile);

            await _BookieDBContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<IActionResult> AssignRoleToUser(SetRoleDto dto)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roleExists = await _RoleManager.RoleExistsAsync(dto.roleName);
            if (!roleExists)
            {
                return BadRequest("Role does not exist");
            }

            var result = await _UserManager.AddToRoleAsync(user, dto.roleName);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Errors);
        }

        [HttpDelete]
        [Route("comment")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult> RemoveComment(DeleteCommentDto dto)
        {
            var comment = await _CommentRepository.GetAsync(dto.commentId, dto.entityId, dto.type);
            if (comment == null) return BadRequest("Komentaras nerastas.");
            await _CommentRepository.DeleteAsync(comment);

            //204
            return NoContent();
        }

        [HttpDelete]
        [Route("book")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult> RemoveBook(DeleteBookDto dto)
        {
            var book = await _BookRepository.GetAsync(dto.bookId);
            if (book == null) return BadRequest("Knyga nerasta.");
            await _BookRepository.DeleteAsync(book);

            //204
            return NoContent();
        }

        [HttpDelete]
        [Route("text")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult> RemoveText(DeleteTextDto dto)
        {
            var text = await _TextRepository.GetAsync(dto.textId);
            if (text == null) return BadRequest("Tekstas nerastas.");
            await _TextRepository.DeleteAsync(text);

            //204
            return NoContent();
        }
    }
}
