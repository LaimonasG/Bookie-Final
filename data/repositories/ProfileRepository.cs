using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.Migrations;
using iText.Commons.Actions.Contexts;
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
        Task<string> UpdatePersonalInfoAsync(PersonalInfoDto dto, BookieUser user,Profile profile);
      //  Task<List<ProfileBookOffersDto>> CalculateBookOffers(Profile profile);
     //   ProfileBookOffersDto CalculateBookOffer(ProfileBook pb);
        ProfileBookOffersDto CalculateBookSubscriptionPrice(ProfileBook pb,Book book);
        //ProfilePurchacesDto GetProfilePurchases(Profile profile);

        List<Tuple<int, int>> ConvertToTupleList(string tupleListString);
        Task RemoveProfileBookAsync(ProfileBook prbo);

        List<ProfileBook> GetProfileBooks(Profile profile);

        Task<ProfileBook> GetProfileBookRecordSubscribed(int bookId,int profileId);

        Task<ProfileBook> GetProfileBookRecordUnSubscribed(int bookId, int profileId);

        Task UpdateProfileBookRecord(ProfileBook pb);

        Task CreateProfileBookRecord(ProfileBook pb);

        string ConvertIdsToString(List<int> data);

        List<int> ConvertStringToIds(string data);

        string ConvertToStringTextDate(Tuple<int, DateTime> tuple);

        bool WasBookSubscribed(ProfileBook prbo);

        bool HasEnoughPoints(double userPoints,double costpoints);

        Task<List<Payment>> GetPaymentList();

        Task PayForPoints(Profile userWallet, Payment payment);

        Task<Payment> GetPayment(int paymentId);

        Task<List<BookDtoBought>> GetBookList(List<ProfileBook> prbo);

        Task<List<PaymentDto>> GetAvailablePayments();

        Task<PaymentDto> CreateAvailablePayment(PaymentCreateDto dto);

    }
    public class ProfileRepository : IProfileRepository
    {
        private const string SystemWalletId = "cf015658-171a-47ca-be37-98f04857c91d";
        private readonly BookieDBContext _BookieDBContext;
        private readonly UserManager<BookieUser> _UserManager;
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
                if (dto.Username.Length > 25 || Regex.IsMatch(dto.Username, @"[^a-zA-Z0-9]"))
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

        //public async Task<List<ProfileBookOffersDto>> CalculateBookOffers(Profile profile)
        //{
        //    List<ProfileBookOffersDto> offersList = new List<ProfileBookOffersDto>();
        //    List<ProfileBook> pb = GetProfileBooks(profile);

        //    if (pb != null)
        //    {
        //        for (int i = 0; i < pb.Count; i++)
        //        {
        //            var offer=CalculateBookOffer(pb.ElementAt(i));
        //            offersList.Add(offer);
        //        }
        //    }

        //    await UpdateAsync(profile);

        //    return offersList;
        //}

        public List<ProfileBook> GetProfileBooks(Profile profile)
        {
            return _BookieDBContext.ProfileBooks.Where(x=>x.ProfileId==profile.Id).ToList();
        }

        //public ProfileBookOffersDto CalculateBookOffer(ProfileBook pb)
        //{
        //    var BoughtChapterList = ConvertStringToIds(pb.BoughtChapterList);
        //    if (BoughtChapterList == null) { BoughtChapterList = new List<int>(); }

        //    var releasedChapters = _BookieDBContext.Chapters
        //                                .Where(x => x.BookId == pb.BookId)
        //                                .Select(t => t.Id)
        //                                .ToList();

        //    var missingChapters=releasedChapters.Except(BoughtChapterList).ToList();

        //    if (missingChapters!=null)
        //    {
        //        if (missingChapters.Count > 0)
        //        {
        //            ProfileBookOffersDto offer = new ProfileBookOffersDto
        //            (pb.BookId, missingChapters);
        //            return offer;
        //        }   
        //    }

        //    ProfileBookOffersDto rez = new ProfileBookOffersDto
        //            (pb.BookId, new List<int>());
        //    return rez;
        //}

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

        //public ProfilePurchacesDto GetProfilePurchases(Profile profile)
        //{
        //    List<ProfileBook> pbs = GetProfileBooks(profile);

        //    List<Tuple<int, int>> bookChapterPairs = pbs
        //            .SelectMany(pb => ConvertStringToIds(pb.BoughtChapterList).Select(chapterId => Tuple.Create(pb.BookId, chapterId)))
        //            .ToList();

        //    List<Tuple<int, int>> TextPurchases = ConvertToTupleList(profile.TextPurchaseDates);

        //    ProfilePurchacesDto result = new ProfilePurchacesDto(bookChapterPairs, TextPurchases);

        //    return result;
        //}

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

        public bool WasBookSubscribed(ProfileBook prbo)
        {        
            return  _BookieDBContext.ProfileBooks.Contains(prbo);
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

        public async Task<List<BookDtoBought>> GetBookList(List<ProfileBook> prbo)
        {
            var bookIds = prbo.Select(pb => pb.BookId).ToList();

            var books = _BookieDBContext.Books.Include(b => b.Chapters)
                                      .Where(x => bookIds.Contains(x.Id))
                                      .ToList();
            List<BookDtoBought> result = new List<BookDtoBought>();
            foreach (Book book in books)
            {

                var temp = new BookDtoBought(
           book.Id,
           book.Name,
           book.Chapters,
           book.GenreName,
           book.Description,
           book.BookPrice,
           book.Created,
           book.UserId,
           book.Author,
           book.CoverImagePath,
           book.IsFinished
              );
                result.Add(temp);

            }

            return result;
           
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
            Payment temp = new Payment { Price = dto.Price, Points = dto.Points };
            _BookieDBContext.Payments.Add(temp);
            await _BookieDBContext.SaveChangesAsync();
            PaymentDto pay = new PaymentDto(temp.Id, temp.Points, temp.Price);
            return pay;
        }
    }
}
