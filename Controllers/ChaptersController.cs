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
        public ChaptersController(IChaptersRepository repo, IAuthorizationService authService, UserManager<BookieUser> userManager)
        {
            _ChapterRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
        }

        [HttpPost]
        public async Task<CreateChapterDto> Create([FromForm]IFormFile file, [FromForm]string chapterName, int bookId)
        {
            string content = _ChapterRepository.ExtractTextFromPDf(file);

            Chapter chapter= new Chapter { Name=chapterName, BookId=bookId,Content= content,UserId= User.FindFirstValue(JwtRegisteredClaimNames.Sub) };
            await _ChapterRepository.CreateAsync(chapter);

            return new CreateChapterDto(chapterName, content);
        }

        [HttpGet]
        [Route("{chapterId}")]
        public async Task<ActionResult<GetChapterDto>> GetOneChapter(int bookId,int chapterId)
        {
            var chapter = await _ChapterRepository.GetAsync(bookId, chapterId);
            if (chapter == null) return NotFound();
            return new GetChapterDto(chapter.Id,chapter.Name,chapter.Content,bookId);
        }

        [HttpGet]
        public async Task<IEnumerable<GetChapterDto>> GetAllChapters(int bookId)
        {
            var chapters = await _ChapterRepository.GetManyAsync(bookId);
            return chapters.Select(x => new GetChapterDto(x.Id,x.Name, x.Content, x.BookId)).Where(y => y.bookId == bookId);
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

            return new GetChapterDto(chapter.Id,chapter.Name, chapter.Content, bookId);
        }
    }
}
