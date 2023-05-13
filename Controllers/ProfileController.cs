using Bakalauras.Auth;
using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        private readonly ITextRepository _Textrepostory;

        public ProfileController(IProfileRepository repo, IAuthorizationService authService,
            UserManager<BookieUser> userManager, IBookRepository repob,
            ITextRepository rept)
        {
            _ProfileRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
            _BookRepository = repob;
            _Textrepostory = rept;
        }

        [HttpGet]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<ProfileDto>> Get()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();

            return new ProfileDto(user.Id, profile.Name, profile.Surname, user.UserName, user.Email, profile.Points);
        }

        [HttpGet]
        [Route("books")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<List<BookDtoBought>>> GetReaderBooks()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var profileBooks = await _ProfileRepository.GetProfileBooks(profile);
            var result =  _ProfileRepository.GetBookList(profileBooks);
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
        public async Task<ActionResult> UpdatePoints(UpdateProfilePointsDto dto, string userId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            if (profile == null) return NotFound();
            var authRez = await _AuthorizationService.AuthorizeAsync(User, profile, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            profile.Points = dto.Points;

            await _ProfileRepository.UpdateAsync(profile);

            return Ok();
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

            string error = await _ProfileRepository.UpdatePersonalInfoAsync(dto, user, profile);
            if (error != null) { return BadRequest(error); }

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

            int chargedUserCount = await _BookRepository.ChargeSubscribersAndUpdateAuthor(dto.bookId, dto.chapterId);

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

            //value hardcoded, until bank API implementation
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
            var rez = await _ProfileRepository.GetAvailablePayments();
            if (rez == null) { return NotFound(); }
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
