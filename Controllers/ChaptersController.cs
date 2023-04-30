using Bakalauras.data.entities;
using Bakalauras.data;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using System.Net.Http.Headers;
using Bakalauras.data.dtos;
using Bakalauras.data;
using static Bakalauras.data.dtos.ChaptersDto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Bakalauras.Auth.Model;
using static Bakalauras.data.repositories.ChaptersRepository;
using Bakalauras.data.repositories;
using System.ComponentModel.Design;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text.RegularExpressions;
using Bakalauras.Migrations;
using Bakalauras.Auth;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/genres/{genreName}/books/{bookId}/chapters")]
    public class ChaptersController:ControllerBase
    {
        private readonly IChaptersRepository _ChapterRepository;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly IBookRepository _BookRepository;
        public ChaptersController(IChaptersRepository repo, IAuthorizationService authService,
            UserManager<BookieUser> userManager, IBookRepository bookRepository)
        {
            _ChapterRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
            _BookRepository = bookRepository;
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieWriter + "," + BookieRoles.Admin)]
        public async Task<ActionResult<CreatedChapterDto>> Create([FromForm] CreateChapterDto dto, int bookId)
        {
            string content = _ChapterRepository.ExtractTextFromPDf(dto.File);
            var book = await _BookRepository.GetAsync(bookId);
            var authRez = await _AuthorizationService.AuthorizeAsync(User, book, PolicyNames.ResourceOwner);
            var UserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            if (content == "error")
            {
                return BadRequest("Failo formatas netinkamas, galima įkelti tik PDF tipo failus.");
            }

            Chapter chapter = new Chapter { Name = dto.Name, BookId = bookId, Content = content, UserId=UserId };
            int chapterId=await _ChapterRepository.CreateAsync(chapter, int.Parse(dto.IsFinished));

            //charge subscribed users
            int chargedUserCount=await _BookRepository.ChargeSubscribersAndUpdateAuthor(bookId, chapterId);

            return new CreatedChapterDto(chapter.Name,chapter.Content,chapter.BookId,chargedUserCount);
        }

        [HttpGet]
        [Route("{chapterId}")]
        [Authorize(Roles = BookieRoles.BookieWriter + "," + BookieRoles.Admin)]
        public async Task<ActionResult<GetChapterDto>> GetOneChapter(int bookId, int chapterId)
        {
            var chapter = await _ChapterRepository.GetAsync(bookId, chapterId);

            var authRez = await _AuthorizationService.AuthorizeAsync(User, chapter, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }
            if (chapter == null) return NotFound();
            return new GetChapterDto(chapter.Name, chapter.Content, chapter.BookId);
        }

        [HttpGet]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<IEnumerable<GetChapterDto>>> GetAllChapters(int bookId)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var hasBook = await _BookRepository.CheckIfUserHasBook(user.Id, bookId);
            if (!hasBook)
            {
                return BadRequest("Naudotojas neturi prieigos prie šių skyrių.");
            }

            var chapters = await _ChapterRepository.GetManyAsync(bookId);

            return Ok(chapters.Select(x => new GetChapterDto(x.Name, x.Content, x.BookId)).Where(y => y.bookId == bookId));
        }

        [HttpDelete]
        [Route("{chapterId}")]
        public async Task<ActionResult> Remove(int chapterId, int bookId)
        {
            var chapter = await _ChapterRepository.GetAsync(chapterId, bookId);
            if (chapter == null) return NotFound();
            await _ChapterRepository.DeleteAsync(chapter);

            //204
            return NoContent();
        }

        [HttpPut]
        [Route("{chapterId}")]
        [Authorize(Roles = $"{BookieRoles.BookieUser},{BookieRoles.Admin}")]
        public async Task<ActionResult<GetChapterDto>> Update(int chapterId, [FromForm] IFormFile? file, [FromForm] string? chapterName, int bookId)
        {
            var chapter = await _ChapterRepository.GetAsync(chapterId, bookId);
            if (chapter == null) return NotFound();
            var authRez = await _AuthorizationService.AuthorizeAsync(User, chapter, PolicyNames.ResourceOwner);
            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            if(chapterName!=null) { chapter.Name = chapterName; }
            if (file != null) { chapter.Content = _ChapterRepository.ExtractTextFromPDf(file); }

            await _ChapterRepository.UpdateAsync(chapter);

            return new GetChapterDto(chapter.Name, chapter.Content, bookId);
        }
    }
}
