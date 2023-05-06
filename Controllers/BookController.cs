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
        private readonly IAuthorizationService _AuthorizationService;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IProfileRepository _ProfileRepository;
        private readonly IChaptersRepository _ChaptersRepository;
        private readonly IConfiguration _configuration;
        private readonly IBookRepository _BookRepository;

        public BookController(IBookRepository repo, IAuthorizationService authServise,
            UserManager<BookieUser> userManager, IProfileRepository profileRepository, IChaptersRepository repp,
            IConfiguration conf)
        {
            _BookRepository = repo;
            _AuthorizationService = authServise;
            _UserManager = userManager;
            _ProfileRepository = profileRepository;
            _ChaptersRepository = repp;
            _configuration = conf;
        }
        [HttpGet]
        [Route("finished")]
        public async Task<IEnumerable<BookDtoToBuy>> GetManyFinished(string GenreName)
        {
            string userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);            
            var books = await _BookRepository.GetManyAsync(GenreName, 1, userId);
            return books;
        }

        [HttpGet]
        [Route("unfinished")]
        public async Task<IEnumerable<BookDtoToBuy>> GetManyUnFinished(string GenreName)
        {
            string userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var books = await _BookRepository.GetManyAsync(GenreName,0,userId);
            return books;
        }

        [HttpGet]
        [Route("{bookId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDtoToBuy>> Get(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);
            if (book.Status == Status.Pateikta) return BadRequest("Knyga dar nebuvo patvirtinta");
            if (book.Status == Status.Atmesta)
            {
                if (book.StatusComment != null)
                    return BadRequest(string.Format("Knyga buvo atmesta, priežastis: {0}", book.StatusComment));
                else
                    return BadRequest("Knyga buvo atmesta.");
            }           

            if (book == null) return NotFound();
            var chapters = await _ChaptersRepository.GetManyAsync(book.Id);
            return new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.BookPrice, book.ChapterPrice,
                chapters.Count, book.Created, book.UserId, await _BookRepository.GetAuthorInfo(book.Id), book.CoverImagePath,
                book.IsFinished);
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
                Status=Status.Pateikta
            };

            if (createBookDto.CoverImage != null)
            {
                using (Stream imageStream = createBookDto.CoverImage.OpenReadStream())
                {
                    string objectKey = $"images/{createBookDto.CoverImage.FileName}";
                    book.CoverImagePath = await _BookRepository.UploadImageToS3Async(imageStream,
                        _configuration["AWS:BucketName"], objectKey, _configuration["AWS:AccessKey"],
                        _configuration["AWS:SecretKey"]);
                }
            }

            await _BookRepository.CreateAsync(book, GenreName);
            var chapters = await _ChaptersRepository.GetManyAsync(book.Id);
            return Created("201", new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.BookPrice, book.ChapterPrice,
                chapters.Count, book.Created, book.UserId, await _BookRepository.GetAuthorInfo(book.Id), book.CoverImagePath,
                book.IsFinished));
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
            book.Status = Status.Pateikta;

            await _BookRepository.UpdateAsync(book);
            var chapters = await _ChaptersRepository.GetManyAsync(book.Id);
            return Ok(new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.BookPrice,book.ChapterPrice,
                chapters.Count, book.Created,book.UserId, await _BookRepository.GetAuthorInfo(book.Id), book.CoverImagePath,
                book.IsFinished));
        }

        [HttpPut]
        [Route("{bookId}/subscribe")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> SubscribeToBook(int bookId)
        {
            var book = await _BookRepository.GetAsync(bookId);

            if (book.Status == Status.Pateikta) return BadRequest("Knyga dar nebuvo patvirtinta");
            if (book.Status == Status.Atmesta)
            {
                if (book.StatusComment != null)
                    return BadRequest(string.Format("Knyga buvo atmesta, priežastis: {0}", book.StatusComment));
                else
                    return BadRequest("Knyga buvo atmesta.");
            }

            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(bookId)).UserId)).Id);

            ProfileBook prbo = new ProfileBook { BookId = bookId, ProfileId = profile.Id, BoughtChapterList = "" };

            //  if (profile.ProfileBooks == null) { profile.ProfileBooks = new List<ProfileBook>(); }

            if (book.UserId == profile.UserId)
            {
                return BadRequest("Jūs esate knygos autorius.");
            }else
            if (_ProfileRepository.WasBookSubscribed(prbo))
            {
                return BadRequest("Knyga jau prenumeruojama.");
            }

            ////add chapters to book and check for an old subscription
            var chapters = await _ChaptersRepository.GetManyAsync(bookId);
            book.Chapters = chapters;
            ProfileBook subscription = await _ProfileRepository.GetProfileBookRecord(bookId, profile.Id,true);
            bool hasOldSub = false;
            var bookPeriodPoints = 0.0;
            if (subscription != null)
            {
                if (subscription.BoughtChapterList != null)
                {
                    List<int> chapterIds= new List<int>();
                    var boughtChapters=_ProfileRepository.ConvertStringToIds(subscription.BoughtChapterList);
                     _BookRepository.HandleBookWasSubscribed(ref chapterIds, boughtChapters, chapters);
                    boughtChapters.AddRange(chapterIds);
                    subscription.BoughtChapterList = _ProfileRepository.ConvertIdsToString(boughtChapters);
                    bookPeriodPoints = book.ChapterPrice * chapterIds.Count;
                    hasOldSub = true;
                }
            }
            else {
                subscription = new ProfileBook { BookId = bookId, ProfileId = profile.Id,BoughtChapterList="" };
                List<int> chapterIds = chapters.Select(x=>x.Id).ToList();
                subscription.BoughtChapterList = _ProfileRepository.ConvertIdsToString(chapterIds);
                bookPeriodPoints = book.ChapterPrice * chapterIds.Count;
            }

            
            if (!_ProfileRepository.HasEnoughPoints(profile.Points, bookPeriodPoints))
            {
                return BadRequest("Pirkiniui nepakanka taškų.");
            }

            profile.Points -= bookPeriodPoints;
            authorProfile.Points += bookPeriodPoints;
           
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

            ProfileBook prbo = await _ProfileRepository.GetProfileBookRecord(bookId, profile.Id,false);

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

            if (book.Status == Status.Pateikta) return BadRequest("Knyga dar nebuvo patvirtinta");
            if (book.Status == Status.Atmesta)
            {
                if (book.StatusComment != null)
                    return BadRequest(string.Format("Knyga buvo atmesta, priežastis: {0}", book.StatusComment));
                else
                    return BadRequest("Knyga buvo atmesta.");
            }

            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(bookId)).UserId)).Id);

            if (book.IsFinished == 0) return BadRequest("Knyga dar nebaigta");

            if (book.UserId == profile.UserId)
            {
                return BadRequest("Jūs esate knygos autorius.");
            }else
            if (await _BookRepository.WasBookBought(book, profile))
            {
                return BadRequest("Naudotojas jau nusipirkęs šią knygą.");
            }else
            if (!_ProfileRepository.HasEnoughPoints(profile.Points, book.BookPrice))
            {
                return BadRequest("Pirkiniui nepakanka taškų.");
            }
            

            ProfileBook pb = new ProfileBook { BookId = bookId, ProfileId = profile.Id };
            var chapters = await _ChaptersRepository.GetManyAsync(bookId);
            book.Chapters = (List<Chapter>)chapters;
            pb.BoughtChapterList = _ProfileRepository.ConvertIdsToString(book.Chapters.Select(x => x.Id).ToList());
            pb.BoughtDate = DateTime.Now;

            authorProfile.Points += book.BookPrice;
            profile.Points -= book.BookPrice;

            await _ProfileRepository.CreateProfileBookRecord(pb);
            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);

            //for testing
            List<SubscribeToBookDto> books = (List<SubscribeToBookDto>)
                await _BookRepository.GetUserSubscribedBooksAsync(profile);

            return Ok(new ProfileBooksDto(user.Id, user.UserName, profile.Points, books));
        }
    }
}
