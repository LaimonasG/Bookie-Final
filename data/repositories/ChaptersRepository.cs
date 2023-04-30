using Bakalauras.data.entities;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text;
using static Bakalauras.data.dtos.ChaptersDto;
using iText.Commons.Actions.Contexts;
using Ganss.Xss;

namespace Bakalauras.data.repositories
{
    public interface IChaptersRepository
    {
        Task<int> CreateAsync(Chapter chapter,int isFinished);
        Task DeleteAsync(Chapter chapter);
        Task<Chapter?> GetAsync(int chapterId, int bookId);
        Task<List<Chapter>> GetManyAsync(int bookId);
     //   Task<List<int>> GetManyChapterIdsAsync(int bookId);
        Task UpdateAsync(Chapter chapter);
        string ExtractTextFromPDf(IFormFile file);
    }

    public class ChaptersRepository : IChaptersRepository
    {
        private readonly BookieDBContext bookieDBContext;
        public ChaptersRepository(BookieDBContext context)
        {
            bookieDBContext = context;
        }

        public async Task<int> CreateAsync(Chapter chapter,int isFinished)
        {
            var book = bookieDBContext.Books.FirstOrDefault(x => x.Id == chapter.BookId);
            chapter.BookId = chapter.BookId;
            book.IsFinished = isFinished;
            bookieDBContext.Chapters.Add(chapter);
            bookieDBContext.Books.Update(book);
            await bookieDBContext.SaveChangesAsync();

            return chapter.Id;
        }

        public async Task<Chapter?> GetAsync(int chapterId, int bookId)
        {
            return await bookieDBContext.Chapters.FirstOrDefaultAsync(x => x.Id == chapterId && x.BookId == bookId);
        }

        public async Task<List<Chapter>> GetManyAsync(int bookId)
        {
            return await bookieDBContext.Chapters.Where(x => x.BookId == bookId).ToListAsync();
        }

        //public async Task<List<int>> GetManyChapterIdsAsync(int bookId)
        //{
        //    var ch = await GetManyAsync(bookId);
        //    return ch.Select(x => x.Id).ToList();
        //}

        public async Task UpdateAsync(Chapter chapter)
        {
            bookieDBContext.Chapters.Update(chapter);
            await bookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Chapter chapter)
        {
            bookieDBContext.Chapters.Remove(chapter);
            await bookieDBContext.SaveChangesAsync();
        }

        public string ExtractTextFromPDf(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName);
            if (fileExtension.ToLower() != ".pdf")
            {
                return "error";
            }

            var fileContent = new StringBuilder();
            using (var reader = new PdfReader(file.OpenReadStream()))
            {
                using (var document = new PdfDocument(reader))
                {
                    for (int i = 1; i < document.GetNumberOfPages() + 1; i++)
                    {
                        var text = PdfTextExtractor.GetTextFromPage(document.GetPage(i));
                        fileContent.Append(text);
                    }
                }
            }

            // Sanitize the content
            var sanitizer = new HtmlSanitizer();
            var sanitizedContent = sanitizer.Sanitize(fileContent.ToString());

            // Replace newlines with HTML line breaks
            var contentWithLineBreaks = sanitizedContent.Replace("\n", "<br>");

            return contentWithLineBreaks;
        }
    }
    }

