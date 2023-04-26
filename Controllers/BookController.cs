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

namespace Bakalauras.Controllers
{

    [ApiController]
    [Route("api/genres/{GenreName}/books")]
    public class BookController : ControllerBase
    {
        private readonly IBookRepository _BookRepository;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IProfileRepository _ProfileRepository;
        private readonly IChaptersRepository _ChaptersRepository;

        public BookController(IBookRepository repo, IAuthorizationService authServise,
            UserManager<BookieUser> userManager, IProfileRepository profileRepository, IChaptersRepository repp)
        {
            _BookRepository = repo;
            _AuthorizationService = authServise;
            _UserManager = userManager;
            _ProfileRepository = profileRepository;
            _ChaptersRepository = repp;
        }
        [HttpGet]
        [Route("finished")]
        public async Task<IEnumerable<BookDtoToBuy>> GetManyFinished(string GenreName)
        {
            var books = await _BookRepository.GetManyAsync();
            return books.Select(x => new BookDtoToBuy(x.Id, x.Name, x.GenreName, x.Description, x.ChapterPrice, DateTime.Now,
                x.UserId,x.Author,x.CoverImagePath, x.IsFinished)).Where(y => y.GenreName == GenreName && y.IsFinished == 1);
        }

        [HttpGet]
        [Route("unfinished")]
        public async Task<IEnumerable<BookDtoToBuy>> GetManyUnFinished(string GenreName)
        {
            var books = await _BookRepository.GetManyAsync();
            return books.Select(x => new BookDtoToBuy(x.Id, x.Name, x.GenreName, x.Description, x.ChapterPrice, DateTime.Now,
                x.UserId, x.Author, x.CoverImagePath,x.IsFinished)).Where(y => y.GenreName == GenreName && y.IsFinished==0);
        }

        [HttpGet]
        [Route("{bookId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDtoToBuy>> Get(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            if (book == null) return NotFound();
            return new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.ChapterPrice, book.Created,
                book.UserId,book.Author,book.CoverImagePath,book.IsFinished);
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieWriter + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDtoToBuy>> Create([FromForm] CreateBookDto createBookDto, string GenreName)
        {
            string userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(userId);
            string author = string.IsNullOrEmpty(profile.Name) || string.IsNullOrEmpty(profile.Surname) ?
                "Autorius nežinomas" : profile.Name + ' ' + profile.Surname;

            var book = new Book
            {
                Name = createBookDto.Name,
                GenreName = GenreName,
                Description = createBookDto.Description,
                ChapterPrice = double.Parse(createBookDto.ChapterPrice),
                BookPrice = double.Parse(createBookDto.BookPrice),
                Created = DateTime.Now,
                UserId = userId,
                Author = author,
            };

            if (createBookDto.CoverImage != null)
            {
                book.CoverImagePath = await _BookRepository.SaveCoverImageBook(createBookDto.CoverImage);
            }

            await _BookRepository.CreateAsync(book, GenreName);

            return Created("201", new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.ChapterPrice,
                book.Created, book.UserId, book.Author, book.CoverImagePath,book.IsFinished));
        }

        [HttpPut]
        [Route("{bookId}")]
        [Authorize(Roles = BookieRoles.BookieWriter + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDtoToBuy>> UpdateAsync(int bookId, UpdateBookDto updateBookDto)
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
            book.Created = updateBookDto.Created;

            await _BookRepository.UpdateAsync(book);

            return Ok(new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.ChapterPrice, book.Created,
                book.UserId, await _BookRepository.GetAuthorInfo(book.Id), book.CoverImagePath,book.IsFinished));
        }

        //[HttpDelete]
        //[Route("{bookId}")]
        //public async Task<ActionResult> Remove(int bookId)
        //{
        //    var book = await _BookRepository.GetAsync(bookId);
        //    if (book == null) return NotFound();
        //    await _BookRepository.DeleteAsync(book);

        //    //204
        //    return NoContent();
        //}

        [HttpPut]
        [Route("{bookId}/subscribe")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> SubscribeToBook(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(bookId)).UserId)).Id);

            ProfileBook prbo = new ProfileBook { BookId = bookId, ProfileId = profile.Id, BoughtChapterList = "" };

            //  if (profile.ProfileBooks == null) { profile.ProfileBooks = new List<ProfileBook>(); }

            if (_ProfileRepository.WasBookSubscribed(prbo, profile))
            {
                return BadRequest("Book was already subscribed!");
            }

            ////add chapters to book and check for an old subscription
            var chapters = await _ChaptersRepository.GetManyAsync(bookId);
            book.Chapters = (List<Chapter>)chapters;
            ProfileBook subscription = await _ProfileRepository.GetProfileBookRecordUnSubscribed(bookId, profile.Id);
            ProfileBookOffersDto bookOffer = new ProfileBookOffersDto(bookId, new List<int>());
            bool hasOldSub = false;
            if (subscription != null)
            {
                if (subscription.BoughtChapterList != null)
                {
                    bookOffer = _ProfileRepository.CalculateBookSubscriptionPrice(subscription, book);
                    hasOldSub = true;
                }
            }
            else { subscription = new ProfileBook { BookId = bookId, ProfileId = profile.Id }; }

            var bookPeriodPoints = book.ChapterPrice * bookOffer.MissingChapters.Count;
            if (!_ProfileRepository.HasEnoughPoints(profile.Points, bookPeriodPoints))
            {
                return BadRequest("Pirkiniui nepakanka taškų.");
            }

            profile.Points -= bookPeriodPoints;
            authorProfile.Points += bookPeriodPoints;

            subscription.BoughtChapterList = _ProfileRepository.ConvertIdsToString(book.Chapters.Select(x => x.Id).ToList());
            subscription.WasUnsubscribed = false;

            if (hasOldSub)
            {
                await _ProfileRepository.UpdateProfileBookRecord(subscription);
            }
            else
            {
                await _ProfileRepository.CreateProfileBookRecord(subscription);
            }

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);

            List<SubscribeToBookDto> books = (List<SubscribeToBookDto>)
                await _BookRepository.GetUserSubscribedBooksAsync(profile);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, profile.Points, books));
        }

        [HttpPut]
        [Route("{bookId}/unsubscribe")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> UnSubscribeToBook(int bookId)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);

            ProfileBook prbo = await _ProfileRepository.GetProfileBookRecordSubscribed(bookId, profile.Id);

            if (prbo == null)
                return BadRequest("Knyga nebuvo prenumeruojama.");

            await _ProfileRepository.RemoveProfileBookAsync(prbo);


            List<SubscribeToBookDto> books = (List<SubscribeToBookDto>)
                await _BookRepository.GetUserSubscribedBooksAsync(profile);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, profile.Points, books));
        }

        [HttpPut]
        [Route("{bookId}/buy")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> BuyBook(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(bookId)).UserId)).Id);

            if (book.IsFinished == 0) return BadRequest("Knyga dar nebaigta");

            if (!_ProfileRepository.HasEnoughPoints(profile.Points, book.BookPrice))
            {
                return BadRequest("Pirkiniui nepakanka taškų.");
            }

            ProfileBook pb = new ProfileBook { BookId = bookId, ProfileId = profile.Id };
            pb.BoughtChapterList = _ProfileRepository.ConvertIdsToString(book.Chapters.Select(x => x.Id).ToList());
            pb.BoughtDate = DateTime.Now;

            authorProfile.Points += book.BookPrice;
            profile.Points -= book.BookPrice;

            await _ProfileRepository.CreateProfileBookRecord(pb);
            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);

            //testavimui, reiks istrint
            List<SubscribeToBookDto> books = (List<SubscribeToBookDto>)
                await _BookRepository.GetUserSubscribedBooksAsync(profile);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, profile.Points, books));
        }
    }
}
