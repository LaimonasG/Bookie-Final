﻿using Microsoft.EntityFrameworkCore;
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
        Task<List<Book>> GetUserBooksAsync(string userId);
        Task<IReadOnlyList<SubscribeToBookDto>> GetUserSubscribedBooksAsync(Profile profile);
        Task<bool> CheckIfUserHasBook(string userId, int bookId);
        Task<List<BookDtoBought>> ConvertBooksToBookDtoBoughtList(List<Book> books);
        Task<string> GetAuthorInfo(int bookId);

        Task<string> SaveCoverImageBook(IFormFile coverImage);
    }

    public class BookRepository : IBookRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IProfileRepository _ProfileRepository;
        private readonly IChaptersRepository _ChaptersRepository;

        public BookRepository(BookieDBContext context, IProfileRepository profileRepository,
            IChaptersRepository chaptersRepository)
        {
            _BookieDBContext = context;
            _ProfileRepository = profileRepository;
            _ChaptersRepository = chaptersRepository;
        }

        public async Task<Book> GetAsync(int bookId)
        {
            return await _BookieDBContext.Books.FirstOrDefaultAsync(x => x.Id == bookId);
        }

        public async Task<IReadOnlyList<Book>> GetManyAsync()
        {
            return await _BookieDBContext.Books.ToListAsync();
        }

        public async Task<List<Book>> GetUserBooksAsync(string userId)
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

        public async Task<bool> CheckIfUserHasBook(string userId, int bookId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            var profileBook= await _BookieDBContext.ProfileBooks
           .SingleOrDefaultAsync(x => x.BookId == bookId && x.ProfileId == profile.Id);
            if (profileBook == null) return false;
            return true;
        }

        public async Task<List<BookDtoBought>> ConvertBooksToBookDtoBoughtList(List<Book> books)
        {
            var bookDtoBoughtList = new List<BookDtoBought>();
            foreach (var book in books)
            {
                var chapters = await _ChaptersRepository.GetManyAsync(book.Id);
                var bookDtoBought = new BookDtoBought(
                    Id: book.Id,
                    Name: book.Name,
                    Chapters: (ICollection<Chapter>)chapters,
                    GenreName: book.GenreName,
                    Description: book.Description,
                    Price: book.BookPrice,
                    Created: book.Created,
                    UserId: book.UserId,
                    Author:await GetAuthorInfo(book.Id),
                    CoverImageUrl: book.CoverImagePath,
                    IsFinished:book.IsFinished
                );
                bookDtoBoughtList.Add(bookDtoBought);
            }
            return bookDtoBoughtList;
        }

        public async Task<string> GetAuthorInfo(int bookId)
        {
            Book book = await GetAsync(bookId);
            var authorProfile = await _ProfileRepository.GetAsync(book.UserId);
            string rez;
            if (authorProfile.Name == null && authorProfile.Surname == null)
                rez = "Anonimas";
            else
                rez = authorProfile.Name + ' ' + authorProfile.Surname;
            return rez;
        }

        public async Task<string> SaveCoverImageBook(IFormFile coverImage)
        {
            if (coverImage == null || coverImage.Length == 0)
            {
                return null;
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(coverImage.FileName);
            var filePath = Path.Combine("../bookie-ui-vite/bookie/public/BookImages", fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await coverImage.CopyToAsync(fileStream);
            }

            return "/BookImages/" + fileName;
        }

        //public async Task<List<Book>> GetFinishedBooks(string genreName)
        //{
        //    return await _BookieDBContext.Books.Where(x=>x.IsFinished==1 && x.GenreName==genreName).ToListAsync();
        //}

        //public async Task<List<Book>> GetUnFinishedBooks(string genreName)
        //{
        //    return await _BookieDBContext.Books.Where(x => x.IsFinished == 0 && x.GenreName == genreName).ToListAsync();
        //}
    }
}
