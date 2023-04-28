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
using System.Text;

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
        private readonly IChaptersRepository _ChaptersRepository;
        private readonly ITextRepository _Textrepostory;

        public ProfileController(IProfileRepository repo, IAuthorizationService authService,
            UserManager<BookieUser> userManager,IBookRepository repob,IChaptersRepository chrep,
            ITextRepository rept)
        {
            _ProfileRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
            _BookRepository = repob;
            _ChaptersRepository = chrep;
            _Textrepostory=rept;
        }

        [HttpGet]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> Get()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            return new ProfileDto(user.Id,profile.Name,profile.Surname, user.UserName, user.Email, profile.Points);
        }

        [HttpGet]
        [Route("books")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<List<BookDtoBought>>> GetReaderBooks()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var profileBooks =  _ProfileRepository.GetProfileBooks(profile);
            var result = await _ProfileRepository.GetBookList(profileBooks);
            if (profile == null) return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [Route("texts")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<List<BookDtoBought>>> GetReaderTexts()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var result = await _Textrepostory.GetUserBoughtTextsAsync(user.Id);
            if (result == null) return NotFound();

            return Ok(result);
        }

        //[HttpGet]
        //[Route("picture")]
        //[Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        //public async Task<ActionResult<ProfileDto>> GetProfilePicture()
        //{
        //    var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
        //    var profile = await _ProfileRepository.GetAsync(user.Id);
        //    if (profile == null) return NotFound();

        //    if (profile.ProfilePicture == null) { return NotFound(); }

        //    return Ok(File(profile.ProfilePicture, "image/png"));
        //}

        [HttpGet]
        [Route("paymentOffers")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<List<ProfileBookOffersDto>>> GetReaderPaymentOffers()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            List<ProfileBookOffersDto> offersList = await _ProfileRepository.CalculateBookOffers(profile);

            return Ok(offersList);
        }

        [HttpGet]
        [Route("payForPoints")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfilePurchacesDto>> GetPointsPaymentOffers()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);

            if (profile == null) return NotFound();

            return Ok(_ProfileRepository.GetProfilePurchases(profile));
        }

        [HttpPut]
        [Route("{userId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> UpdatePoints(UpdateProfilePointsDto dto,string userId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            if (profile == null) return NotFound();
            var user = await _UserManager.FindByIdAsync(profile.UserId);
            var authRez = await _AuthorizationService.AuthorizeAsync(User, profile, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            profile.Points= dto.Points;

            await _ProfileRepository.UpdateAsync(profile);

            return Ok(new ProfileDto(user.Id,profile.Name,profile.Surname, user.UserName, user.Email, profile.Points));
        }

        [HttpPut]
        [Route("info")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<PersonalInfoDto>> UpdatePersonalInfo(PersonalInfoDto dto)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();
            var authRez = await _AuthorizationService.AuthorizeAsync(User, profile, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            string error= await _ProfileRepository.UpdatePersonalInfoAsync(dto, user,profile);
            if(error!=null) { return BadRequest(error); }

            return Ok(new PersonalInfoDto(user.UserName, user.Email,dto.Name,dto.Surname));
        }

        [HttpPut]
        [Route("{userId}/picture")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> UploadProfilePicture(IFormFile file)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            if (file.ContentType != "image/jpeg" && file.ContentType != "image/png")
            {
                return BadRequest("Leistini tik PNG ir JPG formato paveikslėliai.");
            }

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
   
        [HttpPut]
        [Route("{userId}/pay")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileBookPaymentDto>> PayForSubscribtion(ProfilePayDto dto)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _BookRepository.GetAsync(dto.bookId)).UserId)).Id);

            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            var profileBook = await _ProfileRepository.GetProfileBookRecordSubscribed(dto.bookId, profile.Id);
            var book = await _BookRepository.GetAsync(dto.bookId);
            var profileOffer = _ProfileRepository.CalculateBookOffer(profileBook);
            var bookPeriodPoints = book.ChapterPrice * profileOffer.MissingChapters.Count;
            if (profile.Points < bookPeriodPoints)
            {
                return BadRequest("Nepakanka taškų atlikti šį veiksmą.");
            }
     
                profile.Points -= bookPeriodPoints;
                

                var oldPB = await _ProfileRepository.GetProfileBookRecordSubscribed(book.Id, profile.Id);
                var BoughtChapterList = _ProfileRepository.ConvertStringToIds(oldPB.BoughtChapterList);
                if (BoughtChapterList == null) { BoughtChapterList = new List<int>(); }

                BoughtChapterList.AddRange(profileOffer.MissingChapters);
                oldPB.BoughtChapterList = _ProfileRepository.ConvertIdsToString(BoughtChapterList);

                await _ProfileRepository.UpdateProfileBookRecord(oldPB);

                authorProfile.Points += bookPeriodPoints;
            

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);

            return Ok(new ProfileBookPaymentDto(dto.bookId,bookPeriodPoints));
        }

        [HttpPut]
        [Route("payForPoints/{PaymentId}")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfilePurchacesDto>> PayForPoints(int PaymentId)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var payment = await _ProfileRepository.GetPayment(PaymentId);

            if (payment == null) { return NotFound(); }
            if (profile == null) { return NotFound(); }

            bool bankApproval = true;

            if (bankApproval)
            {
                await _ProfileRepository.PayForPoints(profile, payment);
                return Ok();
            }

            return BadRequest("Įvyko banko klaida, bandykite iš naujo.");
        }

    }
}
