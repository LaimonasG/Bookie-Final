using Bakalauras.data.entities;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Bakalauras.data.dtos;

namespace Bakalauras.data.repositories
{
    public interface ITextRepository
    {
        Task CreateAsync(Text Text,string genreName);
        Task<Text?> GetAsync(int TextId);
        Task<IReadOnlyList<Text>> GetManyAsync(string genreName);
        Task UpdateAsync(Text Text);
        Task<List<Text>> GetUserBoughtTextsAsync(string userId);
        Task CreateProfileTextAsync(ProfileText pb);
        Task<bool> WasTextBought(Text text);
        Task<bool> CheckIfUserHasText(string userId, int textId);

        Task<List<Text>> GetUserTextsAsync(string userId);

        Task<List<TextDtoBought>> ConvertTextsTotextDtoBoughtList(List<Text> texts);
    }

    public class TextsRepository : ITextRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IProfileRepository _ProfileRepository;
        public TextsRepository(BookieDBContext context, IProfileRepository profileRepository)
        {
            _BookieDBContext = context;
            _ProfileRepository = profileRepository;
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

        public async Task<List<Text>> GetUserBoughtTextsAsync(string userId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            var profileTexts = await _BookieDBContext.ProfileTexts
                         .Where(pt => pt.ProfileId == profile.Id)
                         .Select(pt => pt.TextId)
                         .ToListAsync();

            if (profileTexts == null)
            {
                return null;
            }
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

        public async Task<bool> CheckIfUserHasText(string userId, int textId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            var profileText = await _BookieDBContext.ProfileTexts
           .SingleOrDefaultAsync(x => x.TextId == textId && x.ProfileId == profile.Id);
            if (profileText == null) return false;
            return true;
        }

        public async Task<List<Text>> GetUserTextsAsync(string userId)
        {
            return await _BookieDBContext.Texts.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task<List<TextDtoBought>> ConvertTextsTotextDtoBoughtList(List<Text> texts)
        {
            var textDtoBoughtList = new List<TextDtoBought>();
            foreach (var text in texts)
            {
                var textDtoBought = new TextDtoBought(
                    Id: text.Id,
                    Name: text.Name,           
                    GenreName: text.GenreName,
                    Content: text.Content,
                    Description: text.Description,
                    Price: text.Price,
                    CoverImageUrl:text.CoverImageUrl,
                    Author:text.Author,
                    Created: text.Created,
                    UserId: text.UserId
                );
                textDtoBoughtList.Add(textDtoBought);
            }
            return textDtoBoughtList;
        }
    }
}
