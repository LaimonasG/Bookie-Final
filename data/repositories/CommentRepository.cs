﻿using Bakalauras.data.entities;
using Microsoft.EntityFrameworkCore;
using Bakalauras.data.dtos;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using static iText.IO.Util.IntHashtable;

namespace Bakalauras.data.repositories
{
    
    public interface ICommentRepository
    {
        Task CreateAsync(Comment cm,int EntityId, string EntityType);
        Task DeleteAsync(Comment cm);
        Task<Comment?> GetAsync(int cmId,int EntityId, string EntityType);
        Task<IReadOnlyList<Comment>> GetManyAsync(int EntityId, string EntityType);
        Task UpdateAsync(Comment cm);
    }

    public class CommentRepository : ICommentRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IBookRepository _BookRepository;
        public CommentRepository(BookieDBContext context)
        {
            _BookieDBContext = context;
        }

        public async Task<Comment?> GetAsync(int cmId, int EntityId, string EntityType)
        {
            return await _BookieDBContext.Comments.FirstOrDefaultAsync(x => x.Id == cmId && x.Type == EntityType
            && x.BookId == EntityId);
        }

        public async Task<IReadOnlyList<Comment>> GetManyAsync(int EntityId, string EntityType)
        {
            return await _BookieDBContext.Comments.Where(c => c.BookId == EntityId && c.Type == EntityType).ToListAsync();
        }

        public async Task CreateAsync(Comment cm, int EntityId, string EntityType)
        {
            Book? book;
            Text? text;

            if (EntityType == "Book") {
                book = _BookieDBContext.Books.FirstOrDefault(x => x.Id == EntityId);
                if (book != null) cm.BookId = EntityId;
                _BookieDBContext.Comments.Add(cm);
                await _BookieDBContext.SaveChangesAsync();
            }
            else if (EntityType == "Text") {
                text = _BookieDBContext.Texts.FirstOrDefault(x => x.Id == EntityId);
                if (text != null) cm.BookId = EntityId;
                _BookieDBContext.Comments.Add(cm);
                await _BookieDBContext.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(Comment cm)
        {
            _BookieDBContext.Comments.Update(cm);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Comment cm)
        {
            _BookieDBContext.Comments.Remove(cm);
            await _BookieDBContext.SaveChangesAsync();
        }
    }
}
