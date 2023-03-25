using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using iText.Layout.Element;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Bcpg;
using PagedList;

namespace Bakalauras.data.repositories
{
    public interface IProfileRepository
    {
        Task CreateAsync(Profile profile);
        Task DeleteAsync(Profile profile);
        Task<Profile?> GetAsync(string userId);
        Task<IReadOnlyList<Profile>> GetManyAsync();
        Task UpdateAsync(Profile profile);
        Task UpdatePersonalInfoAsync(PersonalInfoDto dto);
        Task<List<ProfileBookOffersDto>> CalculateBookOffers(Profile profile);
        ProfilePurchacesDto GetProfilePurchases(Profile profile);
    }
    public class ProfileRepository : IProfileRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IBookRepository _BookRepository;
        private readonly UserManager<BookieUser> _UserManager;
        public ProfileRepository(BookieDBContext context,UserManager<BookieUser> mng, IBookRepository bookRepository)
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

        public async Task UpdatePersonalInfoAsync(PersonalInfoDto dto)
        {
            var user = await _UserManager.FindByIdAsync(dto.userId);

            if(dto.userName!= null) { user.UserName = dto.userName; }
            if (dto.email != null) { user.Email = dto.email; }

            _BookieDBContext.Users.Update(user);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<List<ProfileBookOffersDto>> CalculateBookOffers(Profile profile)
        {
            List<ProfileBookOffersDto> offersList = new List<ProfileBookOffersDto>();
            if (profile.ProfileBooks != null)
            {
                for (int i = 0; i < profile.ProfileBooks.Count; i++)
                {
                    Book book = await _BookRepository.GetAsync(profile.ProfileBooks.ElementAt(i).BookId);
                    DateTime lastPaymentDate = profile.LastBookPaymentDates
                                                .Where(t => t.Item1 == book.Id)
                                                .OrderByDescending(t => t.Item2)
                                                .Select(t => t.Item2)
                                                .FirstOrDefault();

                    if (lastPaymentDate != DateTime.MinValue)
                    {
                        DateTime unpaidChapterReleaseDate = book.Created;
                        while (unpaidChapterReleaseDate < lastPaymentDate)
                        {
                            unpaidChapterReleaseDate = unpaidChapterReleaseDate.AddDays(book.PaymentPeriodDays);
                        }

                        int periodToPayAmount = 0;
                        while (unpaidChapterReleaseDate < DateTime.Now)
                        {
                            unpaidChapterReleaseDate = unpaidChapterReleaseDate.AddDays(book.PaymentPeriodDays);
                            periodToPayAmount += 1;
                        }

                        if (periodToPayAmount > 0)
                        {
                            Tuple<int, DateTime> newDate = new Tuple<int, DateTime>(book.Id, DateTime.Now);
                            profile.LastBookPaymentDates.Add(newDate);
                            ProfileBookOffersDto offer = new ProfileBookOffersDto
                            (book.Id, book.PaymentPeriodDays, periodToPayAmount, book.Price);
                            offersList.Add(offer);
                        }
                    }
                }
            }
            await UpdateAsync(profile);

            return offersList;
        }

        public ProfilePurchacesDto GetProfilePurchases(Profile profile)
        {
            List<Tuple<int, DateTime>> BookPayments = new List<Tuple<int, DateTime>>();
            List<Tuple<int, DateTime>> TextPayments = new List<Tuple<int, DateTime>>();

            foreach (Tuple<int, DateTime> tp in profile.LastBookPaymentDates)
            {
                BookPayments.Add(new Tuple<int, DateTime>(tp.Item1, tp.Item2));
            }

            foreach (Tuple<int, DateTime> tp in profile.TextPurchaseDate)
            {
                TextPayments.Add(new Tuple<int, DateTime>(tp.Item1, tp.Item2));
            }

            ProfilePurchacesDto result = new ProfilePurchacesDto(BookPayments, TextPayments);

            return result;
        }

    }
}
