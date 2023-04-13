using Microsoft.EntityFrameworkCore;
using Bakalauras.data.entities;
using Bakalauras.data;
using Bakalauras.data.dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Bakalauras.Auth.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using static System.Reflection.Metadata.BlobBuilder;

namespace Bakalauras.data.repositories
{
    public interface IBookRepository
    {
        Task CreateAsync(Book book, string genreName);
        Task DeleteAsync(Book book);
        Task<Book> GetAsync(int bookId);
        Task<IReadOnlyList<Book>> GetManyAsync();
        Task UpdateAsync(Book book);

        Task<IReadOnlyList<Book>> GetUserBooksAsync(string userId);

        Task<IReadOnlyList<SubscribeToBookDto>> GetUserSubscribedBooksAsync(Profile profile);
    }

    public class BookRepository : IBookRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly UserManager<BookieUser> _UserManager;

        public BookRepository(BookieDBContext context, UserManager<BookieUser> usrmng)
        {
            _BookieDBContext = context;
            _UserManager = usrmng;
        }

        public async Task<Book> GetAsync(int bookId)
        {
            return await _BookieDBContext.Books.FirstOrDefaultAsync(x => x.Id == bookId);
        }

        public async Task<IReadOnlyList<Book>> GetManyAsync()
        {
            return await _BookieDBContext.Books.ToListAsync();
        }

        public async Task<IReadOnlyList<Book>> GetUserBooksAsync(string userId)
        {
            return await _BookieDBContext.Books.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task<IReadOnlyList<SubscribeToBookDto>> GetUserSubscribedBooksAsync(Profile profile)
        {
            var bookIds = _BookieDBContext.ProfileBooks
                         .Where(pb => pb.ProfileId == profile.Id)
                         .Select(pb => pb.BookId);

            var books = _BookieDBContext.Books
                     .Where(b => bookIds.Contains(b.Id))
                     .ToList();

            List<SubscribeToBookDto> rez = new List<SubscribeToBookDto>();
            foreach(var book in books)
            {
                SubscribeToBookDto temp = new SubscribeToBookDto(book.Id, book.GenreName, book.Name, book.ChapterPrice,
    book.Description, book.Chapters, book.Comments);
                rez.Add(temp);
            }

            return rez;
        }

        public async Task CreateAsync(Book book, string genreName)
        {
            var genre = _BookieDBContext.Genres.FirstOrDefault(x => x.Name == genreName);
            if (genre != null) book.GenreName = genreName;
            _BookieDBContext.Books.Add(book);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Book book)
        {
            _BookieDBContext.Books.Update(book);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Book book)
        {
            _BookieDBContext.Books.Remove(book);
            await _BookieDBContext.SaveChangesAsync();
        }

       

    }
}
