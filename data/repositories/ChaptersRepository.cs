using Bakalauras.data.entities;
using Ganss.Xss;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Bakalauras.data.repositories
{
    public interface IChaptersRepository
    {
        Task<int> CreateAsync(Chapter chapter, int isFinished);
        Task DeleteAsync(Chapter chapter);
        Task<Chapter?> GetAsync(int chapterId, int bookId);
        Task<List<Chapter>> GetManyAsync(int bookId);
        Task UpdateAsync(Chapter chapter);
        string ExtractTextFromPDf(IFormFile file);

        List<int> GetProfilesOwningChapter(int chapterId);

        Task RefundForDeletedChapter(List<int> profiles, double refundAmount);
    }

    public class ChaptersRepository : IChaptersRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IProfileRepository _ProfileRepository;
        public ChaptersRepository(BookieDBContext context, IProfileRepository profileRepository)
        {
            _BookieDBContext = context;
            _ProfileRepository = profileRepository;
        }

        public async Task<int> CreateAsync(Chapter chapter, int isFinished)
        {
            var book = _BookieDBContext.Books.FirstOrDefault(x => x.Id == chapter.BookId);
            if (book == null) return 0;
            book.IsFinished = isFinished;
            _BookieDBContext.Chapters.Add(chapter);
            _BookieDBContext.Books.Update(book);
            await _BookieDBContext.SaveChangesAsync();

            return chapter.Id;
        }

        public async Task<Chapter?> GetAsync(int chapterId, int bookId)
        {
            return await _BookieDBContext.Chapters.FirstOrDefaultAsync(x => x.Id == chapterId && x.BookId == bookId);
        }

        public async Task<List<Chapter>> GetManyAsync(int bookId)
        {
            return await _BookieDBContext.Chapters.Where(x => x.BookId == bookId).ToListAsync();
        }

        public async Task UpdateAsync(Chapter chapter)
        {
            _BookieDBContext.Chapters.Update(chapter);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Chapter chapter)
        {
            double refundAmount = _BookieDBContext.Books
                .Where(y => y.Id == chapter.BookId)
                .Select(x => x.ChapterPrice)
                .FirstOrDefault();

            var profileIds =  GetProfilesOwningChapter(chapter.Id);
            await RefundForDeletedChapter(profileIds, refundAmount);
            _BookieDBContext.Chapters.Remove(chapter);
            await _BookieDBContext.SaveChangesAsync();
        }

        public List<int> GetProfilesOwningChapter(int chapterId)
        {
            var profileBooks = _BookieDBContext.ProfileBooks.ToList();
            List<int> result = new List<int>();
            foreach (var pb in profileBooks)
            {
                if (pb.BoughtChapterList != null)
                {
                    var chapters = _ProfileRepository.ConvertStringToIds(pb.BoughtChapterList);
                    if (chapters.Contains(chapterId))
                        result.Add(pb.ProfileId);
                }
            }
            return result;
        }

        public async Task RefundForDeletedChapter(List<int> profiles, double refundAmount)
        {
            foreach (var profileId in profiles)
            {
                var profile = _BookieDBContext.Profiles.Where(x => x.Id == profileId).FirstOrDefault();
                if (profile == null)
                    return;
                profile.Points += refundAmount;
                _BookieDBContext.Profiles.Update(profile);
                await _BookieDBContext.SaveChangesAsync();
            }
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
                    var strategy = new SimpleTextExtractionStrategy();

                    for (int i = 1; i < document.GetNumberOfPages() + 1; i++)
                    {
                        var text = PdfTextExtractor.GetTextFromPage(document.GetPage(i), strategy);
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

