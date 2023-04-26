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
using System.Text;

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
        private readonly IAuthorizationService _AuthorizationService;

        public TextController(ITextRepository repo, IAuthorizationService authServise, UserManager<BookieUser> mng,
            IProfileRepository pre, IChaptersRepository chrep)
        {
            _Textrepostory = repo;
            _AuthorizationService = authServise;
            _ProfileRepository = pre;
            _UserManager = mng;
            _ChaptersRepository = chrep;
        }

        [HttpGet]
        public async Task<IEnumerable<TextDtoToBuy>> GetMany(string GenreName)
        {
            var texts = await _Textrepostory.GetManyAsync(GenreName);
            return texts.Select(x => new TextDtoToBuy(x.Id, x.Name, x.GenreName,x.Description, x.Price,x.CoverImagePath,
                x.Author, DateTime.Now, x.UserId)).Where(y => y.GenreName == GenreName);
        }

        [HttpGet]
        [Route("{textId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<TextDtoToBuy>> Get(int textId)
        {
            var text = await _Textrepostory.GetAsync(textId);
            if (text == null) return NotFound();
            return new TextDtoToBuy(text.Id, text.Name, text.GenreName,text.Description, text.Price,text.CoverImagePath,
                text.Author, text.Created, text.UserId);
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieWriter + "," + BookieRoles.Admin)]
        public async Task<ActionResult<TextDto>> Create([FromForm] CreateTextDto dto, string genreName)
        {
            string content = _ChaptersRepository.ExtractTextFromPDf(dto.File);
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            string author = string.IsNullOrEmpty(profile.Name) || string.IsNullOrEmpty(profile.Surname) ?
                "Autorius nežinomas" : profile.Name + ' ' + profile.Surname;
            if (content == "error")
            {
                return BadRequest("Failo formatas netinkamas, galima įkelti tik PDF tipo failus.");
            }
            
            Text text = new Text { Name = dto.Name, GenreName=genreName, Content = content,Price=double.Parse(dto.Price),
                UserId = user.Id,Author=author,Description=dto.Description};

            if (dto.CoverImage != null)
            {
                text.CoverImagePath = await _Textrepostory.SaveCoverImageText(dto.CoverImage);
            }


            await _Textrepostory.CreateAsync(text,genreName);

            return new TextDto(text.Name, text.GenreName, text.Content,text.Description,text.Price,text.CoverImagePath,
                text.Created);
        }

        [HttpPut]
        [Route("{textId}")]
        [Authorize(Roles = $"{BookieRoles.BookieWriter},{BookieRoles.Admin}")]
        public async Task<ActionResult<TextDto>> Update([FromForm] UpdateTextDto dto, int textId)
        {
            var text = await _Textrepostory.GetAsync(textId);
            if (text == null) return NotFound();
            var authRez = await _AuthorizationService.AuthorizeAsync(User, text, PolicyNames.ResourceOwner);
            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            if (dto.Name != null) { text.Name = dto.Name; }
            if (dto.File != null) { text.Content = _ChaptersRepository.ExtractTextFromPDf(dto.File); }
            if (double.Parse(dto.Price) != 0) { text.Price = double.Parse(dto.Price); }

            await _Textrepostory.UpdateAsync(text);

            return new TextDto(text.Name, text.GenreName, text.Content, text.Description, text.Price, text.CoverImagePath,
               text.Created);
        }

        //[HttpDelete]
        //[Route("{textId}")]
        //public async Task<ActionResult> Remove(int textId)
        //{
        //    var text = await _Textrepostory.GetAsync(textId);
        //    if (text == null) return NotFound();
        //    await _Textrepostory.DeleteAsync(text);

        //    //204
        //    return NoContent();
        //}

        [HttpPut]
        [Route("{textId}/buy")]
        [Authorize(Roles = $"{BookieRoles.BookieReader},{BookieRoles.Admin}")]
        public async Task<ActionResult<ProfileDto>> PurchaseText(int textId)
        {
            var text = await _Textrepostory.GetAsync(textId);
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _Textrepostory.GetAsync(textId)).UserId)).Id);

            ProfileText prte = new ProfileText { TextId = textId, ProfileId = profile.Id };

            if (profile.Points < text.Price)
            {
                return BadRequest("Pirkiniui nepakanka taškų.");
            }
            else if(await _Textrepostory.WasTextBought(text))
            {
                return BadRequest("Naudotojas jau nusipirkęs šį tekstą.");
            }
            else
            {
                profile.Points -= text.Price;
                authorProfile.Points += text.Price;
                //
                // ar reikia saugot tas datas visgi?>?
                //
              //  Tuple<int, DateTime> newDate = new Tuple<int, DateTime>(textId, DateTime.Now);
              //  StringBuilder temp = new StringBuilder();
              //  temp.Append(profile.TextPurchaseDates);
              //  temp.Append(_ProfileRepository.ConvertToStringTextDate(newDate));
              //  temp.Append(';');
              //  profile.TextPurchaseDates = temp.ToString();
            }
            prte.BoughtDate = DateTime.Now;

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);
            await _Textrepostory.CreateProfileTextAsync(prte);

            List<Text> texts = (List<Text>)await _Textrepostory.GetUserBoughtTextsAsync(profile);

            return Ok(new ProfileTextsDto(user.Id, user.UserName, texts));
        }
    }
}
