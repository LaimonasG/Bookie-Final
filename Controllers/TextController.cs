using Bakalauras.Auth.Model;
using Bakalauras.Auth;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static Bakalauras.data.dtos.ChaptersDto;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/genres/{GenreName}/texts")]
    public class TextController : ControllerBase
    {
        private readonly ITextRepository _Textrepostory;
        private readonly IChaptersRepository _ChaptersRepository;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IProfileRepository _ProfileRepository;
        private readonly IAuthorizationService authorizationService;

        public TextController(ITextRepository repo, IAuthorizationService authServise, IChaptersRepository chaptersRepository)
        {
            _Textrepostory = repo;
            authorizationService = authServise;
            _ChaptersRepository = chaptersRepository;
        }
        [HttpGet]
        public async Task<IEnumerable<TextDto>> GetMany(string GenreName)
        {
            var texts = await _Textrepostory.GetManyAsync(GenreName);
            return texts.Select(x => new TextDto(x.Id, x.Name, x.GenreName, x.Content, x.Price, DateTime.Now, x.UserId)).Where(y => y.GenreName == GenreName);
        }

        [HttpGet]
        [Route("{textId}")]
        public async Task<ActionResult<BookDto>> Get(int textId, string GenreName)
        {
            var text = await _Textrepostory.GetAsync(textId);
            if (text == null) return NotFound();
            return new BookDto(text.Id, text.Name, text.GenreName, text.Content, text.Price, text.Created, text.UserId);
        }

        [HttpPost]
        public async Task<CreateTextDto> Create([FromForm] IFormFile file, [FromForm] string textName, [FromForm] double price, string genreName)
        {
            string content = _ChaptersRepository.ExtractTextFromPDf(file);

            Text text = new Text { Name = textName, GenreName=genreName, Content = content,Price=price, UserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) };
            await _Textrepostory.CreateAsync(text,genreName);

            return new CreateTextDto(textName,content,price);
        }

        [HttpPut]
        [Route("{textId}")]
        [Authorize(Roles = $"{BookieRoles.BookieUser},{BookieRoles.Admin}")]
        public async Task<ActionResult<UpdateTextDto>> Update(int textId, [FromForm] IFormFile? file, [FromForm] string? textName,double Price,string genreName)
        {
            var text = await _Textrepostory.GetAsync(textId);
            if (text == null) return NotFound();
            var authRez = await authorizationService.AuthorizeAsync(User, text, PolicyNames.ResourceOwner);
            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            if (textName != null) { text.Name = textName; }
            if (file != null) { text.Content = _ChaptersRepository.ExtractTextFromPDf(file); }
            if (Price != 0) { text.Price = Price; }

            await _Textrepostory.UpdateAsync(text);

            return new UpdateTextDto(text.Name, text.Content, text.Price);
        }

        [HttpDelete]
        [Route("{textId}")]
        public async Task<ActionResult> Remove(int textId, string GenreName)
        {
            var text = await _Textrepostory.GetAsync(textId);
            if (text == null) return NotFound();
            await _Textrepostory.DeleteAsync(text);

            //204
            return NoContent();
        }

        [HttpPut]
        [Route("{textId}/subscribe")]
        [Authorize(Roles = BookieRoles.BookieReader)]
        public async Task<ActionResult<ProfileDto>> PurchaseText(int textId)
        {
            var text = await _Textrepostory.GetAsync(textId);
            var user = await _UserManager.FindByIdAsync(JwtRegisteredClaimNames.Sub);
            var profile = await _ProfileRepository.GetAsync(user.Id);
            if (profile == null) return NotFound();
            if (text == null) return NotFound();
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _Textrepostory.GetAsync(textId)).UserId)).Id);
            ProfileText prte = new ProfileText { TextId = textId, ProfileId = profile.Id };

            if (profile.ProfileTexts == null) { profile.ProfileTexts = new List<ProfileText>(); }
            if (text.ProfileTexts == null) { text.ProfileTexts = new List<ProfileText>(); }

            if (profile.Points < text.Price)
            {
                return BadRequest("Insufficient points.");
            }
            else if(profile.ProfileTexts.Contains(prte) || text.ProfileTexts.Contains(prte))
            {
                return BadRequest("User already owns the text.");
            }
            else
            {
                profile.Points -= text.Price;
                authorProfile.Points += text.Price;
                profile.TextPurchaseDate.Add(new Tuple<int,DateTime>(textId, DateTime.Now));
            }

            profile.ProfileTexts.Add(prte);
            text.ProfileTexts.Add(prte);

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);
            await _Textrepostory.UpdateAsync(text);

            List<Text> texts = (List<Text>)await _Textrepostory.GetUserTextsAsync(user.Id);

            return Ok(new ProfileTextsDto(user.Id, user.UserName, texts));
        }
    }
}
