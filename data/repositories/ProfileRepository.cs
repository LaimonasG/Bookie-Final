using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using iText.Layout.Element;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Bcpg;
using PagedList;
using System.Linq;
using System.Text;

namespace Bakalauras.data.repositories
{
    public interface IProfileRepository
    {
        Task CreateAsync(Profile profile);
        Task DeleteAsync(Profile profile);
        Task<Profile?> GetAsync(string userId);
        Task<IReadOnlyList<Profile>> GetManyAsync();
        Task UpdateAsync(Profile profile);
        Task UpdatePersonalInfoAsync(PersonalInfoDto dto, string userId);
        Task<List<ProfileBookOffersDto>> CalculateBookOffers(Profile profile);
        ProfileBookOffersDto CalculateBookOffer(ProfileBook pb);
        ProfileBookOffersDto CalculateBookSubscriptionPrice(Book book, string LastBookChapterPayments, List<int> oldSub);
        ProfilePurchacesDto GetProfilePurchases(Profile profile);
        //Tuple<int, int> ConvertToTuple(string tupleString);
        //List<Tuple<int, int>> ConvertToTupleList(string tupleListString);
        Task RemoveProfileBookAsync(ProfileBook prbo);

        public List<ProfileBook> GetProfileBooks(Profile profile);

        Task<ProfileBook> GetProfileBookRecord(int bookId,int profileId);

        Task UpdateProfileBookRecord(ProfileBook pb);

        //string ConvertToString(Tuple<int, int> tuple);

        //string ConvertToStringTextDate(Tuple<int, DateTime> tuple);

    }
    public class ProfileRepository : IProfileRepository
    {
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
            _BookieDBContext.ProfileBooks.Update(prbo);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task UpdatePersonalInfoAsync(PersonalInfoDto dto, string userId)
        {
            var user = await _UserManager.FindByIdAsync(userId);

            if (dto.userName != null) { user.UserName = dto.userName; user.NormalizedUserName = dto.userName; }
            if (dto.email != null) { user.Email = dto.email; user.NormalizedEmail = dto.email; }

            _BookieDBContext.Users.Update(user);
            await _BookieDBContext.SaveChangesAsync();
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
            if (pb.BoughtChapterList == null) { pb.BoughtChapterList = new List<int>(); }

            int lastPaidChapterId = pb.BoughtChapterList
                                             .OrderByDescending(p => p)
                                             .Select(p => p)
                                             .FirstOrDefault();

            int lastReleasedChapterId = _BookieDBContext.Chapters
                                        .Where(x => x.BookId == pb.BookId)
                                        .OrderByDescending(t => t.Id)
                                        .Select(t => t.Id)
                                        .FirstOrDefault();

            if (lastPaidChapterId != lastReleasedChapterId)
            {
                var unpaidChapters = _BookieDBContext.Chapters
                                     .Where(x => x.BookId == pb.BookId)
                                     .Where(ch => ch.Id > lastPaidChapterId && ch.Id <= lastReleasedChapterId)
                                     .ToList();

                if (unpaidChapters.Count > 0)
                {
                    var lastPaidChapter = unpaidChapters
                                      .OrderByDescending(ch => ch.Id)
                                      .Select(t => t.Id)
                                      .FirstOrDefault();
                    ProfileBookOffersDto offer = new ProfileBookOffersDto
                    (pb.BookId, unpaidChapters.Count, unpaidChapters, lastPaidChapter);
                    return offer;
                }
            }

            return null;
        }

        public ProfileBookOffersDto CalculateBookSubscriptionPrice(Book book, string LastBookChapterPayments,List<int> oldSubBooks)
        {
            if (LastBookChapterPayments == null || LastBookChapterPayments == "")
            {
                List<Chapter> chapters = book.Chapters != null ? book.Chapters.ToList() : new List<Chapter>();
                Chapter lastChapter = book?.Chapters?.LastOrDefault();
                if (lastChapter != null)
                {
                    ProfileBookOffersDto offer = new ProfileBookOffersDto
                    (book.Id, chapters.Count, chapters, lastChapter.Id);
                    return offer;
                }
                else
                {
                    ProfileBookOffersDto offer = new ProfileBookOffersDto
                    (book.Id, chapters.Count, chapters, 0);
                    return offer;
                }  
            }
            else
            {
                List<Tuple<int, int>> payments = ConvertToTupleList(LastBookChapterPayments);

                if(oldSubBooks.Count!=0)
                {
                    payments=payments.Where(x=>!oldSubBooks.Contains(x.Item2)).ToList();
                }

                var probo = CalculateBookOffer(book, payments);
                return probo;
            }              
        }

        public ProfilePurchacesDto GetProfilePurchases(Profile profile)
        {
            List<Tuple<int, int>> BookPayments = ConvertToTupleList(profile.LastBookChapterPayments);
            List<Tuple<int, int>> TextPurchases = ConvertToTupleList(profile.TextPurchaseDates);
            List<Tuple<int, int>> BookPaymentsRez = new List<Tuple<int, int>>();
            List<Tuple<int, int>> TextPaymentsRez = new List<Tuple<int, int>>();

            foreach (Tuple<int, int> tp in BookPayments)
            {
                BookPaymentsRez.Add(new Tuple<int, int>(tp.Item1, tp.Item2));
            }

            foreach (Tuple<int, int> tp in TextPurchases)
            {
                TextPaymentsRez.Add(new Tuple<int, int>(tp.Item1, tp.Item2));
            }

            ProfilePurchacesDto result = new ProfilePurchacesDto(BookPaymentsRez, TextPaymentsRez);

            return result;
        }

        //public Tuple<int, int> ConvertToTuple(string tupleString)
        //{
        //    int intVal1 = 0;
        //    int intVal2 = 0;

        //    // check if the input string is in the expected format "(intVal, intVal)"
        //    if (tupleString.StartsWith("(") && tupleString.EndsWith(")"))
        //    {
        //        string[] values = tupleString.Substring(1, tupleString.Length - 2).Split(',');
        //        if (values.Length == 2)
        //        {
        //            int.TryParse(values[0], out intVal1);
        //            int.TryParse(values[1], out intVal2);
        //        }
        //    }

        //    return Tuple.Create(intVal1, intVal2);
        //}

        //public List<Tuple<int, int>> ConvertToTupleList(string tupleListString)
        //{
        //    List<Tuple<int, int>> tupleList = new List<Tuple<int, int>>();

        //    // check if the input string is in the expected format "[ (intVal, dateTimeString); (intVal, dateTimeString); ... ]"
        //    if (tupleListString.StartsWith("[") && tupleListString.EndsWith("]"))
        //    {
        //        string[] tuples = tupleListString.Substring(1, tupleListString.Length - 2).Split(';');
        //        foreach (string tuple in tuples)
        //        {
        //            Tuple<int, int> tupleVal = ConvertToTuple(tuple.Trim());
        //            if (tupleVal != null)
        //            {
        //                tupleList.Add(tupleVal);
        //            }
        //        }
        //    }

        //    return tupleList;
        //}

        public async Task<ProfileBook> GetProfileBookRecord(int bookId, int profileId)
        {
            return (ProfileBook)_BookieDBContext.ProfileBooks.Where(x=>x.BookId==bookId &&x.ProfileId==profileId
            && x.WasUnsubscribed==false);
        }

        public async Task UpdateProfileBookRecord(ProfileBook pb)
        {
             _BookieDBContext.ProfileBooks.Update(pb);
            await _BookieDBContext.SaveChangesAsync();
        }

        //public string ConvertToString(Tuple<int, int> tuple)
        //{
        //    return $"({tuple.Item1}, {tuple.Item2})";
        //}

        //public string ConvertToStringTextDate(Tuple<int, DateTime> tuple)
        //{
        //    return $"({tuple.Item1}, {tuple.Item2:o})";
        //}

    }
}
