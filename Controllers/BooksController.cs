using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Bakalauras.data.repositories;
using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.Auth;

namespace Bakalauras.controllers
{

    [ApiController]
    [Route ("api/genres/{GenreName}/books")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository bookRepository;
        private readonly IAuthorizationService authorizationService;

        public BooksController(IBookRepository repo,IAuthorizationService authServise)
        {
            bookRepository = repo;
            authorizationService = authServise;
        }
        [HttpGet]
        public async Task<IEnumerable<BookDto>> GetMany(string GenreName)
        {
            var books = await bookRepository.GetManyAsync();
            return books.Select(x => new BookDto(x.Id, x.Name, x.GenreName, x.Description,x.Price, DateTime.Now,x.UserId)).Where(y => y.GenreName == GenreName);
        }

        [HttpGet]
        [Route("{bookId}")]
        public async Task<ActionResult<BookDto>> Get(int bookId, string GenreName)
        {
            var book = await bookRepository.GetAsync(bookId, GenreName);
            if (book == null) return NotFound();
            return new BookDto(book.Id, book.Name, book.GenreName, book.Description, book.Price, book.Created, book.UserId);
        }

        [HttpPost]
        [Authorize(Roles=BookieRoles.BookieUser)]
        public async Task<ActionResult<BookDto>> Create(CreateBookDto createBookDto,string GenreName)
        {
            var book = new Book { Name = createBookDto.Name, GenreName= GenreName,
                Description=createBookDto.Description,Price=createBookDto.Price,Created=DateTime.Now,
                UserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) };

            await bookRepository.CreateAsync(book, GenreName);

            //201
            return Created("201", new BookDto(book.Id,book.Name,book.GenreName, book.Description,book.Price, book.Created,book.UserId));
        }

        [HttpPut]
        [Route("{bookId}")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult<BookDto>> Update(int bookId, string GenreName, UpdateBookDto updateBookDto)
        {
            var book = await bookRepository.GetAsync(bookId, GenreName);
            if (book == null) return NotFound();
            var authRez=await authorizationService.AuthorizeAsync(User, book, PolicyNames.ResourceOwner);

            if (!authRez.Succeeded)
            {
                return Forbid();
            }

            book.Name = updateBookDto.Name;
            book.Description = updateBookDto.Description;
            book.Price = updateBookDto.Price;

            await bookRepository.UpdateAsync(book);

            return Ok(new BookDto(book.Id, book.Name, book.GenreName, book.Description,book.Price, book.Created,book.UserId));
        }

        [HttpDelete]
        [Route("{bookId}")]
        public async Task<ActionResult> Remove(int bookId,string GenreName)
        {
            var book = await bookRepository.GetAsync(bookId, GenreName);
            if (book == null) return NotFound();
            await bookRepository.DeleteAsync(book);

            //204
            return NoContent();
        }
    }
}
