using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.data.repositories
{
    public interface ITextRepository
    {
        Task CreateAsync(Text Text, string genreName);
        Task<Text?> GetAsync(int TextId);
        Task<IReadOnlyList<Text>> GetManyAsync(string genreName);
        Task UpdateAsync(Text Text);
        Task<List<ProfileText>> GetProfileTexts(Profile profile);
        Task CreateProfileTextAsync(ProfileText pb);
        Task<bool> WasTextBought(Text text);
        Task<bool> CheckIfUserHasText(string userId, int textId);

        Task<List<Text>> GetUserTextsAsync(string userId);

        List<TextDtoBought> ConvertTextsTotextDtoBoughtList(List<Text> texts);
        Task DeleteAsync(Text text);

        Task<List<TextDtoBought>> GetTextList(List<ProfileText> prbo);
        Task<List<TextDtoBought>> GetSubmittedTextList();

        Task<bool> SetTextStatus(int status, int textId, string statusComment);

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

        public async Task CreateAsync(Text Text, string genreName)
        {
            var genre = _BookieDBContext.Genres.FirstOrDefault(x => x.Name == genreName);
            if (genre != null) Text.GenreName = genreName;
            _BookieDBContext.Texts.Add(Text);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<Text?> GetAsync(int TextId)
        {
            return await _BookieDBContext.Texts.FirstOrDefaultAsync(x => x.Id == TextId);
        }

        public async Task<IReadOnlyList<Text>> GetManyAsync(string genreName)
        {
            return await _BookieDBContext.Texts.Where(x => x.GenreName == genreName && x.Status == Status.Patvirtinta).ToListAsync();
        }

        public async Task DeleteAsync(Text text)
        {
            _BookieDBContext.Texts.Remove(text);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<List<ProfileText>> GetProfileTexts(Profile profile)
        {
            var texts = await _BookieDBContext.Texts.Where(x => x.Status == Status.Patvirtinta).ToListAsync();
            var textIds = texts.Select(x => x.Id).ToList();
            return await _BookieDBContext.ProfileTexts.Where(x => x.ProfileId == profile.Id && textIds.Contains(x.TextId)).ToListAsync();
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
            var found = await _BookieDBContext.ProfileTexts.FirstOrDefaultAsync(x => x.TextId == text.Id);
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

        public List<TextDtoBought> ConvertTextsTotextDtoBoughtList(List<Text> texts)
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
                    CoverImageUrl: text.CoverImageUrl,
                    Author: text.Author,
                    Created: text.Created,
                    UserId: text.UserId,
                    status: text.Status,
                    statusMessage: text.StatusComment
                );
                textDtoBoughtList.Add(textDtoBought);
            }
            return textDtoBoughtList;
        }

        public async Task<List<TextDtoBought>> GetTextList(List<ProfileText> prbo)
        {
            var boughtTexts = new List<TextDtoBought>();

            foreach (var pr in prbo)
            {
                var text = await GetAsync(pr.TextId);
                if (text != null)
                {
                    var textDtoBought = new TextDtoBought
                    (
                        Id: text.Id,
                        Name: text.Name,
                        GenreName: text.GenreName,
                        Content: text.Content,
                        Description: text.Description,
                        Price: text.Price,
                        CoverImageUrl: text.CoverImageUrl,
                        Created: text.Created,
                        UserId: text.UserId,
                        Author: text.Author,
                        status: text.Status,
                        statusMessage: text.StatusComment
                    );

                    boughtTexts.Add(textDtoBought);
                }
            }

            return boughtTexts;
        }

        public async Task<List<TextDtoBought>> GetSubmittedTextList()
        {
            var texts = await _BookieDBContext.Texts.Where(x => x.Status == Status.Pateikta).ToListAsync();
            var textDtos = new List<TextDtoBought>();
            foreach (var text in texts)
            {
                var textDtoBought = new TextDtoBought
                (
                    Id: text.Id,
                    Name: text.Name,
                    GenreName: text.GenreName,
                    Content: text.Content,
                    Description: text.Description,
                    Price: text.Price,
                    CoverImageUrl: text.CoverImageUrl,
                    Created: text.Created,
                    UserId: text.UserId,
                    Author: text.Author,
                    status: text.Status,
                    statusMessage: ""
                );

                textDtos.Add(textDtoBought);
            }
            return textDtos;

        }

        public async Task<bool> SetTextStatus(int status, int textId, string statusComment)
        {
            var text = await GetAsync(textId);
            if (text == null)
            {
                return false;
            }

            Status textStatus = (Status)status;
            text.Status = textStatus;
            text.StatusComment = statusComment;
            await UpdateAsync(text);
            return true;
        }

    }
}
