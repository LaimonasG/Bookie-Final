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
            var profileBooks =  await _ProfileRepository.GetProfileBooks(profile);
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
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var profileTexts = await _Textrepostory.GetProfileTexts(profile);
            var result = await _Textrepostory.GetTextList(profileTexts);
            if (profile == null) return NotFound();

            return Ok(result);
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
        public async Task<ActionResult> UpdatePersonalInfo(PersonalInfoDto dto)
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

            return Ok();
        }

        [HttpPut]
        [Route("pay")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<int>> ChargeUsersForChapter(ProfilePayDto dto)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            int chargedUserCount=await _BookRepository.ChargeSubscribersAndUpdateAuthor(dto.bookId,dto.chapterId);

            return Ok(chargedUserCount);
        }

        [HttpPut]
        [Route("payForPoints/{PaymentId}")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult> PayForPoints(int PaymentId)
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

        [HttpGet]
        [Route("payForPoints")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<PaymentDto>> GetAvailablePayments()
        {
            var rez=await _ProfileRepository.GetAvailablePayments();
            if(rez== null) { return NotFound(); }
            return Ok(rez);
        }

        [HttpPost]
        [Route("payForPoints")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<PaymentDto>> CreateAvailablePayment(PaymentCreateDto dto)
        {
            var rez = await _ProfileRepository.CreateAvailablePayment(dto);
            if (rez == null) { return NotFound(); }
            return Ok(rez);
        }

    }
}
