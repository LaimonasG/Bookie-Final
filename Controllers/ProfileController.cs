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
using System.Net;
using System.Collections.Generic;

namespace Bakalauras.Controllers
{

    [ApiController]
    [Route("api/profiles")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileRepository _ProfileRepository;
        private readonly IBookRepository _BookRepository;
        private readonly ITextRepository _TextRepository;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly UserManager<BookieUser> _UserManager;

        public ProfileController(IProfileRepository repo, IAuthorizationService authService,
            UserManager<BookieUser> userManager,ITextRepository repoText)
        {
            _ProfileRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
            _TextRepository = repoText;
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
        [Route("{userId}/picture")]
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
        [Route("{userId}/picture")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> GetProfilePicture()
        {
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            if (profile.ProfilePicture == null){ return NotFound(); }

            return Ok(File(profile.ProfilePicture, "image/png"));
        }

        [HttpGet]
        [Route("{userId}/paymentOffers")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<List<ProfileBookOffersDto>>> GetReaderPaymentOffers(string userId)
        {
            var user = await _UserManager.FindByIdAsync(userId);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            List<ProfileBookOffersDto> offersList= await _ProfileRepository.CalculateBookOffers(profile);
            
            return Ok(offersList);
        }

        [HttpPut]
        [Route("{userId}/paymentOffers")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileBookPaymentDto>> PayForSubscribtion(ProfileBookOffersDto dto)
        {
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(dto.bookId)).UserId)).Id);

            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();
            var bookPeriodPoints = dto.price * dto.PeriodAmount;
            if (profile.Points < bookPeriodPoints)
            {
                return BadRequest("Insufficient points.");
            }
            else
            {
                profile.Points -= bookPeriodPoints;
                Tuple<int, DateTime> newDate = new Tuple<int, DateTime>(dto.bookId, DateTime.Now);
                profile.LastBookPaymentDates.Add(newDate);
                authorProfile.Points += bookPeriodPoints;
            }

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);

            return Ok(new ProfileBookPaymentDto(dto.bookId,bookPeriodPoints));
        }

        [HttpGet]
        [Route("{userId}/payments")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfilePurchacesDto>> GetReaderPaymentHistory(string userId)
        {
            var user = await _UserManager.FindByIdAsync(userId);
            var profile = await _ProfileRepository.GetAsync(user.Id);

            if (profile == null) return NotFound();

            return Ok(_ProfileRepository.GetProfilePurchases(profile));
        }

    }
}
