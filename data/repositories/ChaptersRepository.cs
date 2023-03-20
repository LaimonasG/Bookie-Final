using Bakalauras.data.entities;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text;
using static Bakalauras.data.dtos.ChaptersDto;

namespace Bakalauras.data.repositories
{
    public interface IChaptersRepository
    {
        Task CreateAsync(Chapter chapter);
        Task DeleteAsync(Chapter chapter);
        Task<Chapter?> GetAsync(int chapterId, int bookId);
        Task<IReadOnlyList<Chapter>> GetManyAsync(int bookId);
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

            public async Task CreateAsync(Chapter chapter)
            {
                var book = bookieDBContext.Books.FirstOrDefault(x => x.Id == chapter.BookId);
                if (book != null) chapter.BookId = chapter.BookId;
                bookieDBContext.Chapters.Add(chapter);
                await bookieDBContext.SaveChangesAsync();
            }

        public async Task<Chapter?> GetAsync(int chapterId, int bookId)
        {
            return await bookieDBContext.Chapters.FirstOrDefaultAsync(x => x.Id == chapterId && x.BookId == bookId);
        }

        public async Task<IReadOnlyList<Chapter>> GetManyAsync(int bookId)
        {
            return await bookieDBContext.Chapters.Where(x => x.BookId == bookId).ToListAsync();
        }

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
            var fileContent = new StringBuilder();
            using (var reader = new PdfReader(file.OpenReadStream()))
            {
                using (var document = new PdfDocument(reader))
                {
                    for (int i = 1; i < document.GetNumberOfPages() + 1; i++)
                    {
                        var text = PdfTextExtractor.GetTextFromPage(document.GetPage(i));
                        text = Regex.Replace(text, "( \\n){2,}", "\n");
                        fileContent.Append(text);
                    }
                }
            }

            return fileContent.ToString();
        }
    }
    }

