using Bakalauras.data.entities;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.data.repositories
{
    public interface ITextRepository
    {
        Task CreateAsync(Text Text,string genreName);
        Task DeleteAsync(Text Text);
        Task<Text?> GetAsync(int TextId);
        Task<IReadOnlyList<Text>> GetManyAsync(string genreName);
        Task UpdateAsync(Text Text);
        Task<IReadOnlyList<Text>> GetUserTextsAsync(Profile profile);
        Task CreateProfileTextAsync(ProfileText pb);
        Task<bool> WasTextBought(Text text);
    }

    public class TextsRepository : ITextRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        public TextsRepository(BookieDBContext context)
        {
            _BookieDBContext = context;           
        }

        public async Task CreateAsync(Text text, string genreName)
        {
            var genre = _BookieDBContext.Genres.FirstOrDefault(x => x.Name == genreName);
            if (genre != null) text.GenreName = genreName;
            _BookieDBContext.Texts.Add(text);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<Text?> GetAsync(int TextId)
        {
            return await _BookieDBContext.Texts.FirstOrDefaultAsync(x => x.Id == TextId);
        }

        public async Task<IReadOnlyList<Text>> GetManyAsync(string genreName)
        {
            return await _BookieDBContext.Texts.Where(x => x.GenreName == genreName).ToListAsync();
        }

        public async Task<IReadOnlyList<Text>> GetUserTextsAsync(Profile profile)
        {
            var profileTexts = await _BookieDBContext.ProfileTexts
                         .Where(pt => pt.ProfileId == profile.Id)
                         .Select(pt => pt.TextId)
                         .ToListAsync();

            var userTexts = await _BookieDBContext.Texts
                         .Where(t => profileTexts.Contains(t.Id))
                         .ToListAsync();
            return userTexts;
        }

        public async Task UpdateAsync(Text Text)
        {
            _BookieDBContext.Texts.Update(Text);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Text Text)
        {
            _BookieDBContext.Texts.Remove(Text);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task CreateProfileTextAsync(ProfileText pb)
        {
            _BookieDBContext.ProfileTexts.Add(pb);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<bool> WasTextBought(Text text)
        {
            var found= await _BookieDBContext.ProfileTexts.FirstOrDefaultAsync(x => x.TextId==text.Id);
            if (found != null) return true;
            return false;
        }
    }
}
