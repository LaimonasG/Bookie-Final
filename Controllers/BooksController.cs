using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Bakalauras.data.repositories;
using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.Auth;
using Microsoft.AspNetCore.Identity;

namespace Bakalauras.controllers
{

    [ApiController]
    [Route ("api/genres/{GenreName}/books")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _BookRepository;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IProfileRepository _ProfileRepository;

        public BooksController(IBookRepository repo,IAuthorizationService authServise,
            UserManager<BookieUser> userManager, IProfileRepository profileRepository)
        {
            _BookRepository = repo;
            _AuthorizationService = authServise;
            _UserManager = userManager;
            _ProfileRepository = profileRepository;
        }
        [HttpGet]
        public async Task<IEnumerable<BookDto>> GetMany(string GenreName)
        {
            var books = await _BookRepository.GetManyAsync();
            return books.Select(x => new BookDto(x.Id, x.Name, x.GenreName, x.Description,x.Price, DateTime.Now,x.UserId)).Where(y => y.GenreName == GenreName);
        }

        [HttpGet]
        [Route("{bookId}")]
        public async Task<ActionResult<BookDto>> Get(int bookId, string GenreName)
        {
            var book = await _BookRepository.GetAsync(bookId);
            if (book == null) return NotFound();
            return new BookDto(book.Id, book.Name, book.GenreName, book.Description, book.Price, book.Created, book.UserId);
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDto>> Create(CreateBookDto createBookDto,string GenreName)
        {
            var book = new Book { Name = createBookDto.Name, GenreName= GenreName,
                Description=createBookDto.Description,Price=createBookDto.Price,Created=DateTime.Now,
                UserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) };

            await _BookRepository.CreateAsync(book, GenreName);

            //201
            return Created("201", new BookDto(book.Id,book.Name,book.GenreName, book.Description,book.Price, book.Created,book.UserId));
        }

        [HttpPut]
        [Route("{bookId}")]
        [Authorize(Roles = BookieRoles.BookieUser+","+ BookieRoles.Admin)]
        public async Task<ActionResult<BookDto>> Update(int bookId, string GenreName, UpdateBookDto updateBookDto)
        {
            var book = await _BookRepository.GetAsync(bookId);
            if (book == null) return NotFound();
            var authRez=await _AuthorizationService.AuthorizeAsync(User, book, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            book.Name = updateBookDto.Name;
            book.Description = updateBookDto.Description;
            book.Price = updateBookDto.Price;

            await _BookRepository.UpdateAsync(book);

            return Ok(new BookDto(book.Id, book.Name, book.GenreName, book.Description,book.Price, book.Created,book.UserId));
        }

        [HttpDelete]
        [Route("{bookId}")]
        public async Task<ActionResult> Remove(int bookId,string GenreName)
        {
            var book = await _BookRepository.GetAsync(bookId);
            if (book == null) return NotFound();
            await _BookRepository.DeleteAsync(book);

            //204
            return NoContent();
        }

        [HttpPut]
        [Route("{bookId}/subscribe")]
        [Authorize(Roles = BookieRoles.BookieReader)]
        public async Task<ActionResult<ProfileDto>> SubscribeToBook(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(bookId)).UserId)).Id);
            if (profile == null) return NotFound();
            if (book == null) return NotFound();
            ProfileBook prbo = new ProfileBook { BookId = bookId, ProfileId = profile.Id };

            var subPrice=_BookRepository.CalculateBookSubscribtionPrice(book);

            var bookPeriodPoints = subPrice.price * subPrice.PeriodAmount;
            if (profile.Points < bookPeriodPoints)
            {
                return BadRequest("Insufficient points.");
            }
            else
            {
                profile.Points -= bookPeriodPoints;
                Tuple<int, DateTime> newDate = new Tuple<int, DateTime>(subPrice.bookId, DateTime.Now);
                profile.LastBookPaymentDates.Add(newDate);
                authorProfile.Points += bookPeriodPoints;
            }

            if (profile.ProfileBooks == null) { profile.ProfileBooks = new List<ProfileBook>(); }
            if (book.ProfileBooks == null) { book.ProfileBooks = new List<ProfileBook>(); }

            if (!profile.ProfileBooks.Contains(prbo))
            {
                profile.ProfileBooks.Add(prbo);
            }

            if (!book.ProfileBooks.Contains(prbo))
            {
                book.ProfileBooks.Add(prbo);
            }

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);
            await _BookRepository.UpdateAsync(book);

            List<Book> books = (List<Book>)await _BookRepository.GetUserBooksAsync(user.Id);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, books));
        }

        [HttpPut]
        [Route("{bookId}/unsubscribe")]
        [Authorize(Roles = BookieRoles.BookieReader)]
        public async Task<ActionResult<ProfileDto>> UnSubscribeToBook(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();
            if (book == null) return NotFound();
            ProfileBook prbo = new ProfileBook { BookId = bookId, ProfileId = profile.Id };

            if (profile.ProfileBooks == null) { return BadRequest("Book was not subscribed"); }
            if (book.ProfileBooks == null) { return BadRequest("Book was not subscribed"); }

            if (profile.ProfileBooks.Contains(prbo))
            {
                profile.ProfileBooks.Remove(prbo);
            }

            if (!book.ProfileBooks.Contains(prbo))
            {
                book.ProfileBooks.Remove(prbo);
            }

            await _ProfileRepository.UpdateAsync(profile);
            await _BookRepository.UpdateAsync(book);

            List<Book> books = (List<Book>)await _BookRepository.GetUserBooksAsync(user.Id);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, books));
        }
    }
}
