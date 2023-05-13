using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/writer")]
    public class WriterPlatformController : ControllerBase
    {
        private readonly IBookRepository _BookRepository;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IWriterPlatformRepository _WriterPlatformRepository;
        private readonly ITextRepository _TextRepository;
        private readonly IChaptersRepository _ChaptersRepository;


        public WriterPlatformController(UserManager<BookieUser> userManager, IBookRepository repob,
            IWriterPlatformRepository writrepo, ITextRepository textRepository, IChaptersRepository chaptersRepository)
        {
            _UserManager = userManager;
            _BookRepository = repob;
            _WriterPlatformRepository = writrepo;
            _TextRepository = textRepository;
            _ChaptersRepository = chaptersRepository;
        }

        [HttpGet]
        [Route("sales")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<WriterSalesData>> GetWriterSales()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));

            WriterSalesData result = new WriterSalesData
                (
                await _WriterPlatformRepository.GetBookData(user.Id),
                await _WriterPlatformRepository.GetTextData(user.Id)
                );

            return Ok(result);
        }

        [HttpGet]
        [Route("getPayment")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<WriterPaymentConfirmation>> GetWriterPayment(double cashOutAmount)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));

            var response = await _WriterPlatformRepository.ProcessWriterPayment(user.Id, cashOutAmount);

            if (response.WithrawalTooSmall)
            {
                return BadRequest($"Išgryninimo suma ({response.EurAmount} Eur) per maža.");
            }

            if (!response.Confirmed)
            {
                return BadRequest("Įvyko banko klaida, bandykite iš naujo.");
            }

            return Ok(response);
        }

        [HttpGet]
        [Route("texts")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<List<TextDtoBought>>> GetManyUsertexts()
        {

            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var texts = await _TextRepository.GetUserTextsAsync(user.Id);

            return Ok(_TextRepository.ConvertTextsTotextDtoBoughtList(texts));
        }

        [HttpGet]
        [Route("texts/{textId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<TextDtoBought>> GetUserText(int textId)
        {
            var text = await _TextRepository.GetAsync(textId);
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            bool hasText = await _TextRepository.CheckIfUserHasText(user.Id, textId);
            if (!hasText) return BadRequest("Naudotojas neturi prieigos prie šio teksto.");
            return new TextDtoBought(text.Id, text.Name, text.GenreName, text.Content, text.Description, text.Price,
                text.CoverImageUrl, text.Author, text.Created, text.UserId, text.Status, text.StatusComment);
        }

        [HttpGet]
        [Route("books")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<List<BookDtoBought>>> GetManyUserBooks()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var books = await _BookRepository.GetUserBooksAsync(user.Id);

            return Ok(await _BookRepository.ConvertBooksToBookDtoBoughtList(books));
        }

        [HttpGet]
        [Route("books/{bookId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDtoBought>> GetUserBook(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            bool hasBook = await _BookRepository.CheckIfUserHasBook(user, bookId);
            if (!hasBook) return BadRequest("Naudotojas neturi prieigos prie šios knygos.");

            var chapters = await _ChaptersRepository.GetManyAsync(bookId);
            return new BookDtoBought(book.Id, book.Name, (ICollection<Chapter>?)chapters, book.GenreName, book.Description,
                book.ChapterPrice, book.BookPrice, book.Created, book.UserId, await _BookRepository.GetAuthorInfo(book.Id),
                book.CoverImagePath, book.IsFinished, book.Status, book.StatusComment);
        }
    }
}
