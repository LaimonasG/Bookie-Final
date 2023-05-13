using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/genres")]
    public class GenresController : ControllerBase
    {
        private readonly IGenreRepository genreRepository;
        public GenresController(IGenreRepository repo)
        {
            genreRepository = repo;
        }

        [HttpGet]
        public async Task<IEnumerable<GenreDto>> GetMany()
        {
            Console.WriteLine("test before");
            var genres = await genreRepository.GetManyAsync();
            Console.WriteLine("test after");
            return genres.Select(x => new GenreDto(x.Id, x.Name));
        }

        [HttpGet]
        [Route("{genreId}")]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<ActionResult<GenreDto>> Get(int genreId)
        {
            var genre = await genreRepository.GetAsync(genreId);
            if (genre == null) return NotFound();
            return new GenreDto(genre.Id, genre.Name);
        }

        [HttpPost]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult<GenreDto>> Create(CreateGenreDto createGendreDto)
        {
            var genre = new Genre { Name = createGendreDto.name };
            await genreRepository.CreateAsync(genre);

            //201
            return Created("201", new GenreDto(genre.Id, genre.Name));
        }

        [HttpPut]
        [Route("{genreId}")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult<GenreDto>> Update(int genreId, UpdateGenreDto updateGenreDto)
        {
            var genre = await genreRepository.GetAsync(genreId);
            if (genre == null) return NotFound();
            genre.Name = updateGenreDto.name;
            await genreRepository.UpdateAsync(genre);

            return Ok(new GenreDto(genre.Id, genre.Name));
        }

        [HttpDelete]
        [Route("{genreId}")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult> Remove(int genreId)
        {
            var genre = await genreRepository.GetAsync(genreId);
            if (genre == null) return NotFound();
            await genreRepository.DeleteAsync(genre);

            //204
            return NoContent();
        }
    }
}
