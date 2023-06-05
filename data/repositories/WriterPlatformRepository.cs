using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.data.repositories
{
    public interface IWriterPlatformRepository
    {
        Task<List<BookSalesData>> GetBookData(string userId);
        Task<List<TextSalesData>> GetTextData(string userId);
        Task<WriterPaymentConfirmation> ProcessWriterPayment(string userId, double withdrawalPercent);
    }
    public class WriterPlatformRepository : IWriterPlatformRepository
    {
        //constants
        private const double _PLATFORMFEEPERCENT = 10;
        private const double _POINTVALUETOEUR = 0.2;
        private const double _MINIMALWITHDRAWALAMT = 5;

        private readonly BookieDBContext _BookieDBContext;
        private readonly IProfileRepository _ProfileRepository;

        public WriterPlatformRepository(BookieDBContext context,IProfileRepository profileRepository)
        {
            _BookieDBContext = context;
            _ProfileRepository = profileRepository;
        }
        public async Task<List<BookSalesData>> GetBookData(string userId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            if(profile == null)
                return new List<BookSalesData>();
            var books = await _BookieDBContext.Books.Where(x => x.UserId == userId && x.Status==Status.Patvirtinta).ToListAsync();
            var bookIds = books.Select(x => x.Id).ToList();
            var profileBooks = await _BookieDBContext.ProfileBooks.Where(pb => bookIds.Contains(pb.BookId)).ToListAsync();
            List<BookSalesData> result = new();
            foreach (var book in books)
            {
                BookSalesData temp = new(
                     book.Name,
                     book.GenreName,
                     book.BookPrice,
                     profileBooks.Count(x => x.BookId == book.Id),
                     profileBooks.Where(x => x.BookId == book.Id).Select(y => y.BoughtDate).ToList(),
                    profileBooks.Count(x => x.BookId == book.Id && !x.WasUnsubscribed));
                result.Add(temp);
            }
            return result;
        }

        public async Task<List<TextSalesData>> GetTextData(string userId)
        {
            var texts = await _BookieDBContext.Texts.Where(x => x.UserId == userId && x.Status == Status.Patvirtinta).ToListAsync();
            var textIds = texts.Select(x => x.Id).ToList();
            var profileTexts = await _BookieDBContext.ProfileTexts.Where(pb => textIds.Contains(pb.TextId)).ToListAsync();
            List<TextSalesData> result = new();
            foreach (var text in texts)
            {
                TextSalesData temp = new(
                     text.Name,
                     text.GenreName,
                     text.Price,
                     profileTexts.Count(x => x.TextId == text.Id),
                     profileTexts.Where(x => x.TextId == text.Id).Select(y => y.BoughtDate).ToList());
                result.Add(temp);
            }
            return result;
        }

        public async Task<WriterPaymentConfirmation> ProcessWriterPayment(string userId, double withdrawalPercent)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            if (profile == null)
                return new WriterPaymentConfirmation(false,true,0,0);
            var pointsToWithdraw = profile.Points * withdrawalPercent*0.01;
            double paymentAmount = (pointsToWithdraw - (pointsToWithdraw * (_PLATFORMFEEPERCENT / 100))) * _POINTVALUETOEUR;
            bool withrawalTooSmall = false;

            if (paymentAmount < _MINIMALWITHDRAWALAMT)
            {
                return new WriterPaymentConfirmation(false, true, 0, paymentAmount);
            }

            //Bank api must process payment, return if it was successful
            bool bankResponse = true;

            if (bankResponse)
            {
                profile.Points -= pointsToWithdraw;
                await _ProfileRepository.UpdateAsync(profile);
            }
            WriterPaymentConfirmation result = new(bankResponse, withrawalTooSmall, pointsToWithdraw, paymentAmount);
            return result;
        }
    }
}
