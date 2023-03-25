using Microsoft.EntityFrameworkCore;
using Bakalauras.data.entities;
using Bakalauras.data;
using Bakalauras.data.dtos;

namespace Bakalauras.data.repositories
{
    public interface IBookRepository
    {
        Task CreateAsync(Book book, string genreName);
        Task DeleteAsync(Book book);
        Task<Book> GetAsync(int bookId);
        Task<IReadOnlyList<Book>> GetManyAsync();

        ProfileBookOffersDto CalculateBookSubscribtionPrice(Book book);
        Task UpdateAsync(Book book);

        Task<IReadOnlyList<Book>> GetUserBooksAsync(string userId);
    }

    public class BookRepository : IBookRepository
    {
        private readonly BookieDBContext bookieDBContext;
        public BookRepository(BookieDBContext context)
        {
            bookieDBContext = context;
        }

        public async Task<Book> GetAsync(int bookId)
        {
            return await bookieDBContext.Books.FirstOrDefaultAsync(x => x.Id == bookId);
        }

        public async Task<IReadOnlyList<Book>> GetManyAsync()
        {
            return await bookieDBContext.Books.ToListAsync();
        }

        public async Task<IReadOnlyList<Book>> GetUserBooksAsync(string userId)
        {
            return await bookieDBContext.Books.Where(x => x.UserId == userId).ToListAsync();
        }

        //public async Task<PagedList<Book>> GetManyAsync(BooksSearchParameters parameters)
        //{
        //    var queryable = bookieDBContext.Books.AsQueryable().OrderBy(o => o.Name);

        //    return await PagedList<Book>.CreateAsync(queryable, parameters.pageNumber,
        //        parameters.PageSize);
        //}

        public async Task CreateAsync(Book book, string genreName)
        {
            var genre = bookieDBContext.Genres.FirstOrDefault(x => x.Name == genreName);
            if (genre != null) book.GenreName = genreName;
            bookieDBContext.Books.Add(book);
            await bookieDBContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Book book)
        {
            bookieDBContext.Books.Update(book);
            await bookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Book book)
        {
            bookieDBContext.Books.Remove(book);
            await bookieDBContext.SaveChangesAsync();
        }

        public ProfileBookOffersDto CalculateBookSubscribtionPrice(Book book)
        {
            DateTime unpaidChapterReleaseDate = book.Created;
            int periodToPayAmount = 0;

            while (unpaidChapterReleaseDate < DateTime.Now)
            {
                unpaidChapterReleaseDate = unpaidChapterReleaseDate.AddDays(book.PaymentPeriodDays);
                periodToPayAmount += 1;
            }

            periodToPayAmount -= 1;

            ProfileBookOffersDto probo = new ProfileBookOffersDto(book.Id,book.PaymentPeriodDays,periodToPayAmount,book.Price);

            return probo;
        }
    }
}
