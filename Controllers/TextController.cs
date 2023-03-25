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

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/genres/{GenreName}/texts")]
    public class TextController : ControllerBase
    {
        private readonly ITextRepository _Textrepostory;
        private readonly IChaptersRepository _ChaptersRepository;
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
            var text = await _Textrepostory.GetAsync(textId, GenreName);
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
            var text = await _Textrepostory.GetAsync(textId, genreName);
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
            var text = await _Textrepostory.GetAsync(textId, GenreName);
            if (text == null) return NotFound();
            await _Textrepostory.DeleteAsync(text);

            //204
            return NoContent();
        }
    }
}
