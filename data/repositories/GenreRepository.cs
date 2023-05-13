using Bakalauras.data.entities;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.data.repositories
{
    public interface IGenreRepository
    {
        Task CreateAsync(Genre genre);
        Task DeleteAsync(Genre genre);
        Task<Genre?> GetAsync(int genreId);
        Task<IReadOnlyList<Genre>> GetManyAsync();
        Task UpdateAsync(Genre genre);
    }

    public class GenreRepository : IGenreRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        public GenreRepository(BookieDBContext context)
        {
            _BookieDBContext = context;
        }

        public async Task<Genre?> GetAsync(int genreId)
        {
            return await _BookieDBContext.Genres.FirstOrDefaultAsync(x => x.Id == genreId);
        }

        public async Task<IReadOnlyList<Genre>> GetManyAsync()
        {
            return await _BookieDBContext.Genres.ToListAsync();
        }

        public async Task CreateAsync(Genre genre)
        {
            _BookieDBContext.Genres.Add(genre);

            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Genre genre)
        {
            _BookieDBContext.Genres.Update(genre);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Genre genre)
        {
            _BookieDBContext.Genres.Remove(genre);
            await _BookieDBContext.SaveChangesAsync();
        }
    }
}
