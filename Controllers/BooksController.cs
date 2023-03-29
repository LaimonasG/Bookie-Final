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
using System.Text;
using Microsoft.EntityFrameworkCore;
using Bakalauras.data;

namespace Bakalauras.controllers
{

    [ApiController]
    [Route("api/genres/{GenreName}/books")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _BookRepository;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IProfileRepository _ProfileRepository;
        private readonly BookieDBContext _BookieDBContext;
        private readonly IChaptersRepository _ChaptersRepository;

        public BooksController(IBookRepository repo, IAuthorizationService authServise,
            UserManager<BookieUser> userManager, IProfileRepository profileRepository, BookieDBContext con,
            IChaptersRepository repp)
        {
            _BookRepository = repo;
            _AuthorizationService = authServise;
            _UserManager = userManager;
            _ProfileRepository = profileRepository;
            _BookieDBContext = con;
            _ChaptersRepository= repp;
        }
        [HttpGet]
        public async Task<IEnumerable<BookDto>> GetMany(string GenreName)
        {
            var books = await _BookRepository.GetManyAsync();
            return books.Select(x => new BookDto(x.Id, x.Name, x.GenreName, x.Description, x.ChapterPrice, DateTime.Now,
                x.UserId)).Where(y => y.GenreName == GenreName);
        }

        [HttpGet]
        [Route("{bookId}")]
        public async Task<ActionResult<BookDto>> Get(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            if (book == null) return NotFound();
            return new BookDto(book.Id, book.Name, book.GenreName, book.Description, book.ChapterPrice, book.Created,
                book.UserId);
        }


        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDto>> Create(CreateBookDto createBookDto, string GenreName)
        {
            var book = new Book
            {
                Name = createBookDto.Name,
                GenreName = GenreName,
                Description = createBookDto.Description,
                ChapterPrice = createBookDto.Price,
                Created = DateTime.Now,
                UserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            };

            await _BookRepository.CreateAsync(book, GenreName);

            //201
            return Created("201", new BookDto(book.Id, book.Name, book.GenreName, book.Description, book.ChapterPrice,
                book.Created, book.UserId));
        }

        //testavimui galima nustatyti sukurimo data
        [HttpPut]
        [Route("{bookId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDto>> Update(int bookId, UpdateBookDto updateBookDto)
        {
            var book = await _BookRepository.GetAsync(bookId);
            if (book == null) return NotFound();
            var authRez = await _AuthorizationService.AuthorizeAsync(User, book, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            book.Name = updateBookDto.Name;
            book.Description = updateBookDto.Description;
            book.ChapterPrice = updateBookDto.Price;
            book.Created = updateBookDto.Created;

            await _BookRepository.UpdateAsync(book);

            return Ok(new BookDto(book.Id, book.Name, book.GenreName, book.Description, book.ChapterPrice, book.Created,
                book.UserId));
        }

        [HttpDelete]
        [Route("{bookId}")]
        public async Task<ActionResult> Remove(int bookId)
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
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(bookId)).UserId)).Id);
            if (profile == null) return NotFound();
            if (book == null) return NotFound();
            ProfileBook prbo = new ProfileBook { BookId = bookId, ProfileId = profile.Id,BoughtChapterList=new List<int>() };

            

            if (profile.ProfileBooks == null) { profile.ProfileBooks = new List<ProfileBook>(); }

            if (profile.ProfileBooks.Contains(prbo))
            {
                return BadRequest("Book was already subscribed!");
            }
            var chapters=await _ChaptersRepository.GetManyAsync(bookId);
            book.Chapters = (List<Chapter>)chapters;
            ProfileBook OldSubscription = await _ProfileRepository.GetProfileBookRecord(bookId, profile.Id);
            ProfileBookOffersDto bookOffer = new ProfileBookOffersDto { bookId = book.Id, ChapterAmount = 0 };

            if (OldSubscription!= null)
            {
                if(OldSubscription.BoughtChapterList != null)
                {
                    bookOffer = _ProfileRepository.CalculateBookSubscriptionPrice(OldSubscription);
                }     
            }
           

            var bookPeriodPoints = book.ChapterPrice * bookOffer.ChapterAmount;
            if (profile.Points < bookPeriodPoints)
            {
                return BadRequest("Insufficient points.");
            }
            else
            {
                profile.Points -= bookPeriodPoints;
                Tuple<int, int> newChapter = new Tuple<int, int>(bookOffer.bookId, 0);
                StringBuilder temp = new StringBuilder();
                temp.Append(profile.LastBookChapterPayments);
                temp.Append(_ProfileRepository.ConvertToString(newChapter));
                temp.Append(';');
                profile.LastBookChapterPayments = temp.ToString();
                authorProfile.Points += bookPeriodPoints;
            }
            List<int> chapterIds = await _ChaptersRepository.GetManyChapterIdsAsync(bookId);
            prbo.BoughtChapterList.AddRange(chapterIds);
            profile.ProfileBooks.Add(prbo);

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);
            await _BookRepository.UpdateAsync(book);

            List<SubscribeToBookDto> books = (List<SubscribeToBookDto>)
                await _BookRepository.GetUserSubscribedBooksAsync(profile);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, profile.Points, books));
        }

        [HttpPut]
        [Route("{bookId}/unsubscribe")]
        [Authorize(Roles = BookieRoles.BookieReader)]
        public async Task<ActionResult<ProfileDto>> UnSubscribeToBook(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();
            if (book == null) return NotFound();

            ProfileBook prbo = new ProfileBook { BookId = bookId, ProfileId = profile.Id,WasUnsubscribed=true };

            try { await _ProfileRepository.RemoveProfileBookAsync(prbo); }
            catch { return BadRequest("Book wasn't subscribed."); }

            List<SubscribeToBookDto> books = (List<SubscribeToBookDto>)
                await _BookRepository.GetUserSubscribedBooksAsync(profile);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, profile.Points, books));
        }
    }
}
