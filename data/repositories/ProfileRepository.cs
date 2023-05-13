using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Bakalauras.data.repositories
{
    public interface IProfileRepository
    {
        Task CreateAsync(Profile profile);
        Task<Profile?> GetAsync(string userId);
        Task UpdateAsync(Profile profile);
        Task<string> UpdatePersonalInfoAsync(PersonalInfoDto dto, BookieUser user, Profile profile);

        Task RemoveProfileBookAsync(ProfileBook prbo);

        Task<List<ProfileBook>> GetProfileBooks(Profile profile);

        Task<ProfileBook> GetProfileBookRecord(int bookId, int profileId, bool isSubscribed);

        Task UpdateProfileBookRecord(ProfileBook pb);

        Task CreateProfileBookRecord(ProfileBook pb);

        string ConvertIdsToString(List<int> data);

        List<int> ConvertStringToIds(string data);

        bool WasBookSubscribed(ProfileBook prbo);

        bool HasEnoughPoints(double userPoints, double costpoints);

        Task PayForPoints(Profile userWallet, Payment payment);

        Task<Payment> GetPayment(int paymentId);

        List<BookDtoBought> GetBookList(List<ProfileBook> prbo);

        Task<List<PaymentDto>> GetAvailablePayments();

        Task<PaymentDto> CreateAvailablePayment(PaymentCreateDto dto);

        Task<List<Profile>> GetBookSubscribers(int bookId);
    }
    public class ProfileRepository : IProfileRepository
    {
        private const string _SystemWalletId = "cf015658-171a-47ca-be37-98f04857c91d";
        private readonly BookieDBContext _BookieDBContext;
        public ProfileRepository(BookieDBContext context)
        {
            _BookieDBContext = context;
        }

        public async Task<Profile?> GetAsync(string userId)
        {
            return await _BookieDBContext.Profiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<IReadOnlyList<Profile>> GetManyAsync()
        {
            return await _BookieDBContext.Profiles.ToListAsync();
        }

        public async Task CreateAsync(Profile profile)
        {
            _BookieDBContext.Profiles.Add(profile);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Profile profile)
        {
            _BookieDBContext.Profiles.Update(profile);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Profile profile)
        {
            _BookieDBContext.Profiles.Remove(profile);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task RemoveProfileBookAsync(ProfileBook prbo)
        {
            prbo.WasUnsubscribed = true;
            _BookieDBContext.ProfileBooks.Update(prbo);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<string> UpdatePersonalInfoAsync(PersonalInfoDto dto, BookieUser user, Profile profile)
        {
            bool badEmail = false;
            bool badUsername = false;
            bool badName = false;
            bool badSurname = false;
            string UsernameEmailError = "Vartotojo vardo ir elektroninio pašto formatai neteisingi.";
            string UsernameError = "Vartotojo vardo formatas neteisingas.";
            string nameError = "Vardo formatas neteisingas.";
            string surnameError = "Pavardės formatas neteisingas.";
            string emailError = "Elektroninio pašto formatas neteisingas.";

            if (dto.Username != null)
            {
                if (dto.Username.Length > 25 || Regex.IsMatch(dto.Username, @"[^a-zA-Z0-9ąčęėįšųūžĄČĘĖĮŠŲŪŽ]"))
                {
                    badUsername = true;
                }
                user.UserName = dto.Username;
                user.NormalizedUserName = dto.Username.ToUpper();
            }
            if (dto.Email != null)
            {
                if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    badEmail = true;
                }
                user.Email = dto.Email;
                user.NormalizedEmail = dto.Email;
            }
            if (dto.Name != null)
            {
                if (dto.Name.Length > 25)
                {
                    badName = true;
                }
                profile.Name = dto.Name;
            }
            if (dto.Surname != null)
            {
                if (dto.Surname.Length > 25)
                {
                    badSurname = true;
                }
                profile.Surname = dto.Surname;
            }
            if (badEmail && badUsername) { return UsernameEmailError; }
            else if (badEmail) { return emailError; }
            else if (badUsername) { return UsernameError; }
            else if (badName) { return nameError; }
            else if (badSurname) { return surnameError; }

            _BookieDBContext.Profiles.Update(profile);
            _BookieDBContext.Users.Update(user);
            await _BookieDBContext.SaveChangesAsync();

            return null;
        }

        public async Task<List<ProfileBook>> GetProfileBooks(Profile profile)
        {
            var books = await _BookieDBContext.Books.Where(x => x.Status == Status.Patvirtinta).ToListAsync();
            var bookIds = books.Select(x => x.Id).ToList();
            return await _BookieDBContext.ProfileBooks.Where(x => x.ProfileId == profile.Id && bookIds.Contains(x.BookId)).ToListAsync();
        }

        public async Task<ProfileBook> GetProfileBookRecord(int bookId, int profileId, bool isSubscribed)
        {
            return await _BookieDBContext.ProfileBooks
           .FirstOrDefaultAsync(x => x.BookId == bookId && x.ProfileId == profileId && x.WasUnsubscribed == isSubscribed);
        }

        public async Task UpdateProfileBookRecord(ProfileBook pb)
        {
            _BookieDBContext.ProfileBooks.Update(pb);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task CreateProfileBookRecord(ProfileBook pb)
        {
            _BookieDBContext.ProfileBooks.Add(pb);
            await _BookieDBContext.SaveChangesAsync();
        }


        public string ConvertIdsToString(List<int> data)
        {
            if (data == null || data.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new ();
            foreach (int id in data)
            {
                sb.Append(id);
                sb.Append(',');
            }

            sb.Length--;
            return sb.ToString();
        }

        public List<int> ConvertStringToIds(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new List<int>();
            }

            return data.Split(',').Select(int.Parse).ToList();
        }

        public bool WasBookSubscribed(ProfileBook prbo)
        {
            var pb = _BookieDBContext.ProfileBooks.Where(x => !x.WasUnsubscribed && x.BookId == prbo.BookId).FirstOrDefault();
            return (pb != null);
        }

        public bool HasEnoughPoints(double userPoints, double costpoints)
        {
            return userPoints >= costpoints;
        }

        public async Task<Payment> GetPayment(int paymentId)
        {
            return await _BookieDBContext.Payments.FirstOrDefaultAsync(x => x.Id == paymentId);
        }

        public async Task PayForPoints(Profile userWallet, Payment payment)
        {
            PaymentUser pu = new() { PaymentId = payment.Id, ProfileId = userWallet.Id, Date = DateTime.Now };
            Profile SystemWallet = await GetAsync(_SystemWalletId);
            SystemWallet.Points += payment.Price;
            userWallet.Points += payment.Points;

            await UpdateAsync(SystemWallet);
            await UpdateAsync(userWallet);
            _BookieDBContext.PaymentUsers.Add(pu);
            await _BookieDBContext.SaveChangesAsync();
        }

        //returns only the chapters that were bought previously
        public List<BookDtoBought> GetBookList(List<ProfileBook> prbo)
        {
            var removeunsUnsubcribed = prbo.Where(x => !x.WasUnsubscribed).ToList();
            var bookIds = removeunsUnsubcribed.Select(pb => pb.BookId).ToList();
            var boughtChaptersMap = new Dictionary<int, List<int>>();

            foreach (var temp in removeunsUnsubcribed)
            {
                var ids = ConvertStringToIds(temp.BoughtChapterList);
                boughtChaptersMap[temp.BookId] = ids;
            }

            var books = _BookieDBContext.Books.Include(b => b.Chapters)
                                  .Where(x => bookIds.Contains(x.Id))
                                  .ToList();
            return (from book in books
                    where boughtChaptersMap.ContainsKey(book.Id)
                    let boughtChapters = book.Chapters.Where(c => boughtChaptersMap[book.Id].Contains(c.Id)).ToList()
                    let temp = new BookDtoBought(
                    book.Id,
                    book.Name,
                    boughtChapters,
                    book.GenreName,
                    book.Description,
                    book.ChapterPrice,
                    book.BookPrice,
                    book.Created,
                    book.UserId,
                    book.Author,
                    book.CoverImagePath,
                    book.IsFinished,
                    book.Status,
                    book.StatusComment
                    )
                    select temp).ToList();
        }

        public async Task<List<PaymentDto>> GetAvailablePayments()
        {
            var payments = await _BookieDBContext.Payments
               .Select(p => new PaymentDto(p.Id, p.Points, p.Price))
               .ToListAsync();
            return payments;
        }

        public async Task<PaymentDto> CreateAvailablePayment(PaymentCreateDto dto)
        {
            Payment temp = new() { Price = dto.Price, Points = dto.Points };
            _BookieDBContext.Payments.Add(temp);
            await _BookieDBContext.SaveChangesAsync();
            PaymentDto pay = new (temp.Id, temp.Points, temp.Price);
            return pay;
        }

        public async Task<List<Profile>> GetBookSubscribers(int bookId)
        {
            var profileBooks = await _BookieDBContext.ProfileBooks.Where(x => x.BookId == bookId).ToListAsync();
            var profileIds = profileBooks.Select(pb => pb.ProfileId).ToList();
            var profiles = await _BookieDBContext.Profiles.Where(x => profileIds.Contains(x.Id)).ToListAsync();
            return profiles;
        }
    }
}
