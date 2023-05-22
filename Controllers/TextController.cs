using Bakalauras.Auth;
using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        private readonly IConfiguration _configuration;
        private readonly IBookRepository _BookRepository;

        public TextController(ITextRepository repo, IAuthorizationService authServise, UserManager<BookieUser> mng,
            IProfileRepository pre, IChaptersRepository chrep, IConfiguration conf, IBookRepository repob)
        {
            _Textrepostory = repo;
            _AuthorizationService = authServise;
            _ProfileRepository = pre;
            _UserManager = mng;
            _ChaptersRepository = chrep;
            _configuration = conf;
            _BookRepository = repob;
        }

        [HttpGet]
        public async Task<IEnumerable<TextDtoToBuy>> GetMany(string GenreName)
        {
            var texts = await _Textrepostory.GetManyAsync(GenreName);
            return texts.Select(x => new TextDtoToBuy(x.Id, x.Name, x.GenreName, x.Description, x.Price, x.CoverImageUrl,
                x.Author, x.Created, x.UserId)).Where(y => y.GenreName == GenreName);
        }

        [HttpGet]
        [Route("{textId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<TextDtoToBuy>> Get(int textId)
        {
            var text = await _Textrepostory.GetAsync(textId);
            if (text == null) return NotFound();

            if (text.Status == Status.Pateikta) return BadRequest("Tekstas dar nebuvo patvirtintas");
            if (text.Status == Status.Atmesta)
            {
                if (text.StatusComment != null)
                    return BadRequest(string.Format("Tekstas buvo atmestas, priežastis: {0}", text.StatusComment));
                else
                    return BadRequest("Tekstas buvo atmestas.");
            }

            return new TextDtoToBuy(text.Id, text.Name, text.GenreName, text.Description, text.Price, text.CoverImageUrl,
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
            else if (content.Length > 100000)
            {
                return BadRequest("Failo simbolių kiekis viršytas.");
            }

            Text text = new Text
            {
                Name = dto.Name,
                GenreName = genreName,
                Content = content,
                Price = double.Parse(dto.Price),
                UserId = user.Id,
                Author = author,
                Description = dto.Description,
                Created = DateTime.Now,
                Status = Status.Pateikta
            };

            if (dto.CoverImage != null)
            {
                using (Stream imageStream = dto.CoverImage.OpenReadStream())
                {
                    string objectKey = $"images/{dto.CoverImage.FileName}";
                    text.CoverImageUrl = await _BookRepository.UploadImageToS3Async(imageStream,
                        _configuration["AWS:BucketName"], objectKey, _configuration["AWS:AccessKey"],
                        _configuration["AWS:SecretKey"]);
                }
            }


            await _Textrepostory.CreateAsync(text, genreName);

            return new TextDto(text.Name, text.GenreName, text.Content, text.Description, text.Price, text.CoverImageUrl,
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

            text.Name = dto.Name;
            text.Content = _ChaptersRepository.ExtractTextFromPDf(dto.File);
            text.Price = double.Parse(dto.Price);
            text.Created = DateTime.Now;
            text.Status = Status.Pateikta;

            if (dto.CoverImage != null)
            {
                using (Stream imageStream = dto.CoverImage.OpenReadStream())
                {
                    string objectKey = $"images/{dto.CoverImage.FileName}";
                    await _BookRepository.DeleteImageFromS3Async(text.CoverImageUrl, _configuration["AWS:AccessKey"],
                        _configuration["AWS:SecretKey"]);
                    text.CoverImageUrl = await _BookRepository.UploadImageToS3Async(imageStream,
                        _configuration["AWS:BucketName"], objectKey, _configuration["AWS:AccessKey"],
                        _configuration["AWS:SecretKey"]);
                }
            }

            await _Textrepostory.UpdateAsync(text);

            return new TextDto(text.Name, text.GenreName, text.Content, text.Description, text.Price, text.CoverImageUrl,
               text.Created);
        }

        [HttpPut]
        [Route("{textId}/buy")]
        [Authorize(Roles = $"{BookieRoles.BookieReader},{BookieRoles.Admin}")]
        public async Task<ActionResult> PurchaseText(int textId)
        {
            var text = await _Textrepostory.GetAsync(textId);
            if (text == null) return NotFound();

            if (text.Status == Status.Pateikta) return BadRequest("Tekstas dar nebuvo patvirtintas");
            if (text.Status == Status.Atmesta)
            {
                if (text.StatusComment != null)
                    return BadRequest(string.Format("Tekstas buvo atmestas, priežastis: {0}", text.StatusComment));
                else
                    return BadRequest("Tekstas buvo atmestas.");
            }

            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null)
            {
                return BadRequest("Norint pirkti tekstą, turite prisijungti.");
            }
            var profile = await _ProfileRepository.GetAsync(user.Id);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync((
                                await _Textrepostory.GetAsync(textId)).UserId)).Id);

            ProfileText prte = new ProfileText { TextId = textId, ProfileId = profile.Id };

          
            if (text.UserId == profile.UserId)
            {
                return BadRequest("Jūs esate teksto autorius.");
            }
            else if (await _Textrepostory.WasTextBought(text, profile.Id))
            {
                return BadRequest("Naudotojas jau nusipirkęs šį tekstą.");
            }
            else if (profile.Points < text.Price)
            {
                return BadRequest("Pirkiniui nepakanka taškų.");
            }
            profile.Points -= text.Price;
            authorProfile.Points += text.Price;

            prte.BoughtDate = DateTime.Now;

            await _ProfileRepository.UpdateAsync(profile);
            await _ProfileRepository.UpdateAsync(authorProfile);
            await _Textrepostory.CreateProfileTextAsync(prte);

            //200
            return Ok();
        }
    }
}
