using Bakalauras.Auth.Model;
using Bakalauras.Auth;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bakalauras.Controllers
{

    [ApiController]
    [Route("api/profiles")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileRepository _ProfileRepository;
        private readonly IBookRepository _BookRepository;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly UserManager<BookieUser> _UserManager;

        public ProfileController(IProfileRepository repo, IAuthorizationService authService, UserManager<BookieUser> userManager)
        {
            _ProfileRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
        }
        [HttpGet]
        public async Task<IEnumerable<ProfileDto>> GetMany()
        {
            var profiles = await _ProfileRepository.GetManyAsync();

            var usersAndProfiles = from profile in profiles
                                   join user in _UserManager.Users
                                   on profile.UserId equals user.Id
                                   select new { User = user, Profile = profile };

            return profiles.Select(x => new ProfileDto(x.User.Id,x.User.UserName,x.User.Email,x.Points));
        }

        [HttpGet]
        [Route("{userId}")]
        public async Task<ActionResult<ProfileDto>> Get(string userId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            if (profile == null) return NotFound();
            var user = await _UserManager.FindByIdAsync(userId);

            return new ProfileDto(user.Id, user.UserName, user.Email, profile.Points);
        }

        //[HttpPost]
        //[Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        //public async Task<ActionResult<ProfileDto>> Create(CreateProfileDto dto)
        //{
        //    var profile = new Profile{Points=0,UserId=dto.userId};

        //    await _ProfileRepository.CreateAsync(profile);
        //    var user = await _UserManager.FindByIdAsync(profile.UserId);

        //    //201
        //    return Created("201", new ProfileDto(user.Id, user.UserName, user.Email, profile.Points));
        //}

        [HttpPut]
        [Route("{userId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<BookDto>> Update(UpdateProfileDto dto)
        {
            var profile = await _ProfileRepository.GetAsync(dto.userId);
            if (profile == null) return NotFound();
            var user = await _UserManager.FindByIdAsync(profile.UserId);
            var authRez = await _AuthorizationService.AuthorizeAsync(User, profile, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            profile.Points= dto.points;

            await _ProfileRepository.UpdateAsync(profile);

            return Ok(new ProfileDto(user.Id, user.UserName, user.Email, profile.Points));
        }


        [HttpPut]
        [Authorize(Roles = BookieRoles.BookieReader)]
        public async Task<ActionResult<ProfileDto>> SubscribeToBook(SubscribeToBookDto dto)
        {
            var book = await _BookRepository.GetAsync(dto.bookId, dto.genreName);
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();
            if (book == null) return NotFound();
            ProfileBook prbo = new ProfileBook { BookId = dto.bookId, ProfileId = profile.Id };

            if (profile.ProfileBooks == null){ profile.ProfileBooks = new List<ProfileBook>(); }
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
            await _BookRepository.UpdateAsync(book);

            List<Book> books= (List<Book>)await _BookRepository.GetUserBooksAsync(user.Id);

            return Ok(new ProfileBooksDto(user.Id,user.UserName,books));
        }

        [HttpPut]
        [Authorize(Roles = BookieRoles.BookieReader)]
        public async Task<ActionResult<ProfileDto>> UnSubscribeToBook(SubscribeToBookDto dto)
        {
            var book = await _BookRepository.GetAsync(dto.bookId, dto.genreName);
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();
            if (book == null) return NotFound();
            ProfileBook prbo = new ProfileBook { BookId = dto.bookId, ProfileId = profile.Id };

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

        [HttpPut]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> UploadProfilePicture(IFormFile file)
        {
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Update the profile object with the uploaded file data
            profile.ProfilePicture = fileBytes;

            await _ProfileRepository.UpdateAsync(profile);


            return Ok();
        }

        [HttpGet]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> GetProfilePicture()
        {
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            if (profile.ProfilePicture == null){ return NotFound(); }

            return Ok(File(profile.ProfilePicture, "image/png"));
        }

    }
}
