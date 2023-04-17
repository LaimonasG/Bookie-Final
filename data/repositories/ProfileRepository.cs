using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using iText.Layout.Element;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Bcpg;
using PagedList;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Reflection.Metadata.BlobBuilder;

namespace Bakalauras.data.repositories
{
    public interface IProfileRepository
    {
        Task CreateAsync(Profile profile);
        Task DeleteAsync(Profile profile);
        Task<Profile?> GetAsync(string userId);
        Task<IReadOnlyList<Profile>> GetManyAsync();
        Task UpdateAsync(Profile profile);
        Task<string> UpdatePersonalInfoAsync(PersonalInfoDto dto, string userId);
        Task<List<ProfileBookOffersDto>> CalculateBookOffers(Profile profile);
        ProfileBookOffersDto CalculateBookOffer(ProfileBook pb);
        ProfileBookOffersDto CalculateBookSubscriptionPrice(ProfileBook pb,Book book);
        ProfilePurchacesDto GetProfilePurchases(Profile profile);

        List<Tuple<int, int>> ConvertToTupleList(string tupleListString);
        Task RemoveProfileBookAsync(ProfileBook prbo);

        public List<ProfileBook> GetProfileBooks(Profile profile);

        Task<ProfileBook> GetProfileBookRecordSubscribed(int bookId,int profileId);

        Task<ProfileBook> GetProfileBookRecordUnSubscribed(int bookId, int profileId);

        Task UpdateProfileBookRecord(ProfileBook pb);

        Task CreateProfileBookRecord(ProfileBook pb);

        string ConvertIdsToString(List<int> data);

        List<int> ConvertStringToIds(string data);

        string ConvertToStringTextDate(Tuple<int, DateTime> tuple);

        bool WasBookSubscribed(ProfileBook prbo, Profile profile);

        bool HasEnoughPoints(double userPoints,double costpoints);

        Task<List<Payment>> GetPaymentList();

        Task PayForPoints(Profile userWallet, Payment payment);

        Task<Payment> GetPayment(int paymentId);

        Task<List<BookSalesData>> GetBookData(string userId);

        Task<List<TextSalesData>> GetTextData(string userId);

    }
    public class ProfileRepository : IProfileRepository
    {
        private const string SystemWalletId = "cf015658-171a-47ca-be37-98f04857c91d";
        private readonly BookieDBContext _BookieDBContext;
        private readonly IBookRepository _BookRepository;
        private readonly UserManager<BookieUser> _UserManager;
        public ProfileRepository(BookieDBContext context, UserManager<BookieUser> mng, IBookRepository bookRepository)
        {
            _BookieDBContext = context;
            _UserManager = mng;
            _BookRepository = bookRepository;
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

        public async Task<string> UpdatePersonalInfoAsync(PersonalInfoDto dto, string userId)
        {
            var user = await _UserManager.FindByIdAsync(userId);
            bool badEmail = false;
            bool badUsername=false;
            string usernameEmailError = "Vartotojo vardo ir elektroninio pašto formatai neteisingi.";
            string usernameError = "Vartotojo vardo formatas neteisingas.";
            string emailError = "Elektroninio pašto formatas neteisingas.";

            if (dto.userName != null)
            {
                if (dto.userName.Length > 25 && Regex.IsMatch(dto.userName, @"[^a-zA-Z0-9]"))
                {
                    badUsername=true;
                }
                user.UserName = dto.userName;
                user.NormalizedUserName = dto.userName.ToUpper();
            }
            if (dto.email != null)
            {
                if (!Regex.IsMatch(dto.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    badEmail = true;
                }
                user.Email = dto.email;
                user.NormalizedEmail = dto.email;
            }
            if (badEmail && badUsername) { return usernameEmailError; } else
            if (badEmail) { return emailError; } else if(badUsername) { return usernameError; }

            _BookieDBContext.Users.Update(user);
            await _BookieDBContext.SaveChangesAsync();

            return null;
        }

        public async Task<List<ProfileBookOffersDto>> CalculateBookOffers(Profile profile)
        {
            List<ProfileBookOffersDto> offersList = new List<ProfileBookOffersDto>();
            List<ProfileBook> pb = GetProfileBooks(profile);

            if (pb != null)
            {
                for (int i = 0; i < pb.Count; i++)
                {
                    var offer=CalculateBookOffer(pb.ElementAt(i));
                    offersList.Add(offer);
                }
            }

            await UpdateAsync(profile);

            return offersList;
        }

        public List<ProfileBook> GetProfileBooks(Profile profile)
        {
            return _BookieDBContext.ProfileBooks.Where(x=>x.ProfileId==profile.Id).ToList();
        }

        public ProfileBookOffersDto CalculateBookOffer(ProfileBook pb)
        {
            var BoughtChapterList = ConvertStringToIds(pb.BoughtChapterList);
            if (BoughtChapterList == null) { BoughtChapterList = new List<int>(); }

            var releasedChapters = _BookieDBContext.Chapters
                                        .Where(x => x.BookId == pb.BookId)
                                        .Select(t => t.Id)
                                        .ToList();

            var missingChapters=releasedChapters.Except(BoughtChapterList).ToList();

            if (missingChapters!=null)
            {
                if (missingChapters.Count > 0)
                {
                    ProfileBookOffersDto offer = new ProfileBookOffersDto
                    (pb.BookId, missingChapters);
                    return offer;
                }   
            }

            ProfileBookOffersDto rez = new ProfileBookOffersDto
                    (pb.BookId, new List<int>());
            return rez;
        }

        public ProfileBookOffersDto CalculateBookSubscriptionPrice(ProfileBook pb,Book book)
        {
            List<Chapter> chapters = book.Chapters != null ? book.Chapters.ToList() : new List<Chapter>();
            if (pb.BoughtChapterList.Count()==0)
            {
                if (chapters.Count != null)
                {
                    List<int> ids=chapters.Select(x=>x.Id).ToList();
                    ProfileBookOffersDto offer = new ProfileBookOffersDto
                    (book.Id, ids);
                    return offer;
                }
                else
                {
                    ProfileBookOffersDto offer = new ProfileBookOffersDto
                    (book.Id, new List<int>());
                    return offer;
                }  
            }
            else
            {
                var probo = CalculateBookOffer(pb);
                return probo;
            }              
        }

        public ProfilePurchacesDto GetProfilePurchases(Profile profile)
        {
            List<ProfileBook> pbs = GetProfileBooks(profile);

            List<Tuple<int, int>> bookChapterPairs = pbs
                    .SelectMany(pb => ConvertStringToIds(pb.BoughtChapterList).Select(chapterId => Tuple.Create(pb.BookId, chapterId)))
                    .ToList();

            List<Tuple<int, int>> TextPurchases = ConvertToTupleList(profile.TextPurchaseDates);

            ProfilePurchacesDto result = new ProfilePurchacesDto(bookChapterPairs, TextPurchases);

            return result;
        }

        public Tuple<int, int> ConvertToTuple(string tupleString)
        {
            int intVal1 = 0;
            int intVal2 = 0;

            // check if the input string is in the expected format "(intVal, intVal)"
            if (tupleString.StartsWith("(") && tupleString.EndsWith(")"))
            {
                string[] values = tupleString.Substring(1, tupleString.Length - 2).Split(',');
                if (values.Length == 2)
                {
                    int.TryParse(values[0], out intVal1);
                    int.TryParse(values[1], out intVal2);
                }
            }

            return Tuple.Create(intVal1, intVal2);
        }

        public List<Tuple<int, int>> ConvertToTupleList(string tupleListString)
        {
            List<Tuple<int, int>> tupleList = new List<Tuple<int, int>>();

            if(tupleListString!=null)
            if (tupleListString.StartsWith("[") && tupleListString.EndsWith("]"))
            {
                string[] tuples = tupleListString.Substring(1, tupleListString.Length - 2).Split(';');
                foreach (string tuple in tuples)
                {
                    Tuple<int, int> tupleVal = ConvertToTuple(tuple.Trim());
                    if (tupleVal != null)
                    {
                        tupleList.Add(tupleVal);
                    }
                }
            }

            return tupleList;
        }

        public async Task<ProfileBook> GetProfileBookRecordSubscribed(int bookId, int profileId)
        {
            return await _BookieDBContext.ProfileBooks
           .SingleOrDefaultAsync(x => x.BookId == bookId && x.ProfileId == profileId && x.WasUnsubscribed == false);
        }

        public async Task<ProfileBook> GetProfileBookRecordUnSubscribed(int bookId, int profileId)
        {
            return await _BookieDBContext.ProfileBooks
           .SingleOrDefaultAsync(x => x.BookId == bookId && x.ProfileId == profileId && x.WasUnsubscribed == true);
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

        public string ConvertToStringTextDate(Tuple<int, DateTime> tuple)
        {
            return $"({tuple.Item1}, {tuple.Item2:o})";
        }

        public string ConvertIdsToString(List<int> data)
        {
            if (data == null || data.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            foreach (int id in data)
            {
                sb.Append(id);
                sb.Append(",");
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

        public bool WasBookSubscribed(ProfileBook prbo, Profile profile)
        {
            return profile.ProfileBooks.Contains(prbo);
        }

        public bool HasEnoughPoints(double userPoints, double costpoints)
        {
            return userPoints >= costpoints;
        }

        public async Task<List<Payment>> GetPaymentList()
        {
            return await _BookieDBContext.Payments.ToListAsync();
        }

        public async Task<Payment> GetPayment(int paymentId)
        {
            return await _BookieDBContext.Payments.FirstOrDefaultAsync(x => x.Id == paymentId);
        }

        public async Task PayForPoints(Profile userWallet, Payment payment)
        {
            PaymentUser pu = new PaymentUser { PaymentId = payment.Id, ProfileId = userWallet.Id, Date = DateTime.Now };
            Profile SystemWallet = await GetAsync(SystemWalletId);
            SystemWallet.Points += payment.Price;
            userWallet.Points += payment.Points;

            await UpdateAsync(SystemWallet);
            await UpdateAsync(userWallet);
            _BookieDBContext.PaymentUsers.Add(pu);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<List<BookSalesData>> GetBookData(string userId)
        {
            var profile = await GetAsync(userId);
            var books = await _BookieDBContext.Books.Where(x => x.UserId == userId).ToListAsync();
            var bookIds=books.Select(x=>x.Id).ToList();
            var profileBooks= await _BookieDBContext.ProfileBooks.Where(pb => bookIds.Contains(pb.BookId)).ToListAsync();
            List<BookSalesData> result = new List<BookSalesData>();
            foreach(var book in books){
                BookSalesData temp = new BookSalesData(
                     book.Name,
                     book.BookPrice,
                     profileBooks.Count(x => x.BookId == book.Id),
                     profileBooks.Where(x => x.BookId == book.Id).Select(y => y.BoughtDate).ToList(),
                    profileBooks.Count(x => x.BookId==book.Id && x.WasUnsubscribed==false));
                result.Add(temp);
            }
            return result;
        }

        public async Task<List<TextSalesData>> GetTextData(string userId)
        {
            var profile = await GetAsync(userId);
            var texts = await _BookieDBContext.Texts.Where(x => x.UserId == userId).ToListAsync();
            var textIds = texts.Select(x => x.Id).ToList();
            var profileTexts = await _BookieDBContext.ProfileTexts.Where(pb => textIds.Contains(pb.TextId)).ToListAsync();
            List<TextSalesData> result = new List<TextSalesData>();
            foreach (var text in texts)
            {
                TextSalesData temp = new TextSalesData(
                     text.Name,
                     text.Price,
                     profileTexts.Count(x => x.TextId == text.Id),
                     profileTexts.Where(x => x.TextId == text.Id).Select(y => y.BoughtDate).ToList());
                result.Add(temp);
            }
            return result;
        }
    }
}
