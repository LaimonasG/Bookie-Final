using Microsoft.EntityFrameworkCore;
using Bakalauras.data.entities;
using Bakalauras.data;
using Bakalauras.data.dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Bakalauras.Auth.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using static System.Reflection.Metadata.BlobBuilder;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Bakalauras.Migrations;
using System.Text.RegularExpressions;

namespace Bakalauras.data.repositories
{
    public interface IBookRepository
    {
        Task CreateAsync(Book book, string genreName);
        Task DeleteAsync(Book book);
        Task<Book> GetAsync(int bookId);
        Task<IReadOnlyList<BookDtoToBuy>> GetManyAsync(string genreName,int isFinished,string userId);
        Task UpdateAsync(Book book);
        Task<List<Book>> GetUserBooksAsync(string userId);
        Task<IReadOnlyList<SubscribeToBookDto>> GetUserSubscribedBooksAsync(Profile profile);
        Task<bool> CheckIfUserHasBook(string userId, int bookId);
        Task<List<BookDtoBought>> ConvertBooksToBookDtoBoughtList(List<Book> books);
        Task<string> GetAuthorInfo(int bookId);

        Task<bool> WasBookBought(Book book,Profile profile);

        Task<int> ChargeSubscribersAndUpdateAuthor(int bookId, int chapterId);

        void HandleBookWasSubscribed(ref List<int> chapterIds, List<int> BoughtChapterList, List<Chapter> actualChapters);
        (string bucketName, string objectKey) ParseS3Url(string imageUrl);
        Task<bool> DeleteImageFromS3Async(string imageUrl, string accessKey, string secretKey);
        Task<string> UploadImageToS3Async(Stream imageStream, string bucketName, string objectKey, string accessKey, string secretKey);
    }

    public class BookRepository : IBookRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IProfileRepository _ProfileRepository;
        private readonly IChaptersRepository _ChaptersRepository;
        private readonly UserManager<BookieUser> _UserManager;


        public BookRepository(BookieDBContext context, IProfileRepository profileRepository,
            IChaptersRepository chaptersRepository,UserManager<BookieUser> mng)
        {
            _BookieDBContext = context;
            _ProfileRepository = profileRepository;
            _ChaptersRepository = chaptersRepository;
            _UserManager= mng;
        }

        public async Task<Book> GetAsync(int bookId)
        {
            return await _BookieDBContext.Books.FirstOrDefaultAsync(x => x.Id == bookId);
        }

        public async Task<IReadOnlyList<BookDtoToBuy>> GetManyAsync(string genreName,int isFinished, string userId)
        {
            var books = await _BookieDBContext.Books.ToListAsync();
            var bookDtos = new List<BookDtoToBuy>();
            var profile = await _ProfileRepository.GetAsync(userId);
            
            foreach (var book in books)
            {
                int chapterCount = 0;
                var chapters = await _ChaptersRepository.GetManyAsync(book.Id);
                ProfileBook? pb = await _ProfileRepository.GetProfileBookRecord(book.Id, profile.Id, true);
                if (pb != null)
                {
                    List<int> chapterIds = new List<int>();
                    var boughtChapters = _ProfileRepository.ConvertStringToIds(pb.BoughtChapterList);
                    HandleBookWasSubscribed(ref chapterIds, boughtChapters, chapters);

                    chapterCount = chapterIds.Count;
                    var bookDto = new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.BookPrice,
                    book.ChapterPrice, chapterCount, book.Created, book.UserId, book.Author, book.CoverImagePath,
                    book.IsFinished);
                    bookDtos.Add(bookDto);
                }
                else
                {
                    var bookDto = new BookDtoToBuy(book.Id, book.Name, book.GenreName, book.Description, book.BookPrice,
                    book.ChapterPrice, chapters.Count, book.Created, book.UserId, book.Author, book.CoverImagePath,
                    book.IsFinished);
                    bookDtos.Add(bookDto);
                }                                         
            }
            return bookDtos.Where(y => y.GenreName == genreName && y.IsFinished == isFinished).ToList();
        }

        public async Task<List<Book>> GetUserBooksAsync(string userId)
        {
            return await _BookieDBContext.Books.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task<IReadOnlyList<SubscribeToBookDto>> GetUserSubscribedBooksAsync(Profile profile)
        {
            var bookIds = _BookieDBContext.ProfileBooks
                         .Where(pb => pb.ProfileId == profile.Id)
                         .Select(pb => pb.BookId);

            var books = _BookieDBContext.Books
                     .Where(b => bookIds.Contains(b.Id))
                     .ToList();

            List<SubscribeToBookDto> rez = new List<SubscribeToBookDto>();
            foreach(var book in books)
            {
                SubscribeToBookDto temp = new SubscribeToBookDto(book.Id, book.GenreName, book.Name, book.ChapterPrice,
    book.Description, book.Chapters, book.Comments);
                rez.Add(temp);
            }

            return rez;
        }

        public async Task CreateAsync(Book book, string genreName)
        {
            var genre = _BookieDBContext.Genres.FirstOrDefault(x => x.Name == genreName);
            if (genre != null) book.GenreName = genreName;
            _BookieDBContext.Books.Add(book);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Book book)
        {
            _BookieDBContext.Books.Update(book);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Book book)
        {
            _BookieDBContext.Books.Remove(book);
            await _BookieDBContext.SaveChangesAsync();
        }

        public async Task<bool> CheckIfUserHasBook(string userId, int bookId)
        {
            var profile = await _ProfileRepository.GetAsync(userId);
            var profileBook= await _BookieDBContext.ProfileBooks
           .SingleOrDefaultAsync(x => x.BookId == bookId && x.ProfileId == profile.Id);
            var book = await GetAsync(bookId);
            if (profileBook == null && (book.UserId !=userId)) return false;
            return true;
        }

        public async Task<List<BookDtoBought>> ConvertBooksToBookDtoBoughtList(List<Book> books)
        {
            var bookDtoBoughtList = new List<BookDtoBought>();
            foreach (var book in books)
            {
                var chapters = await _ChaptersRepository.GetManyAsync(book.Id);
                var bookDtoBought = new BookDtoBought(
                    Id: book.Id,
                    Name: book.Name,
                    Chapters: (ICollection<Chapter>)chapters,
                    GenreName: book.GenreName,
                    Description: book.Description,
                    Price: book.BookPrice,
                    Created: book.Created,
                    UserId: book.UserId,
                    Author:await GetAuthorInfo(book.Id),
                    CoverImageUrl: book.CoverImagePath,
                    IsFinished:book.IsFinished
                );
                bookDtoBoughtList.Add(bookDtoBought);
            }
            return bookDtoBoughtList;
        }

        public async Task<string> GetAuthorInfo(int bookId)
        {
            Book book = await GetAsync(bookId);
            var authorProfile = await _ProfileRepository.GetAsync(book.UserId);
            string rez;
            if (authorProfile.Name == null && authorProfile.Surname == null)
                rez = "Anonimas";
            else
                rez = authorProfile.Name + ' ' + authorProfile.Surname;
            return rez;
        }

        public async Task<string> UploadImageToS3Async(Stream imageStream, string bucketName, string objectKey, string accessKey, string secretKey)
        {
            // Generate a unique object key by adding a GUID prefix
            objectKey = $"{Guid.NewGuid().ToString()}_{objectKey}";

            // Create an Amazon S3 client using the access key and secret key
            var s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.EUNorth1);

            // Create a TransferUtility to handle the upload
            var transferUtility = new TransferUtility(s3Client);

            // Create a TransferUtilityUploadRequest with the bucket name and object key
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = imageStream,
                BucketName = bucketName,
                Key = objectKey,
                CannedACL = S3CannedACL.PublicRead,
            };

            // Upload the image
            await transferUtility.UploadAsync(uploadRequest);

            // Get the URL of the uploaded image
            string imageUrl = $"https://{bucketName}.s3.eu-north-1.amazonaws.com/{objectKey}";

            return imageUrl;
        }


        public async Task<bool> DeleteImageFromS3Async(string imageUrl, string accessKey, string secretKey)
        {
            try
            {
                // Parse the imageUrl to get the bucketName and objectKey
                (string bucketName, string objectKey) = ParseS3Url(imageUrl);

                // Create an Amazon S3 client using the access key and secret key
                var s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.EUNorth1);

                // Create a DeleteObjectRequest with the bucket name and object key
                var deleteRequest = new Amazon.S3.Model.DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                };

                // Delete the image
                await s3Client.DeleteObjectAsync(deleteRequest);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting image from S3: " + ex.Message);
                return false;
            }
        }

        public (string bucketName, string objectKey) ParseS3Url(string imageUrl)
        {
            // Remove the "https://" prefix
            imageUrl = imageUrl.Substring(8);

            // Split the URL into parts
            string[] urlParts = imageUrl.Split('/');

            // Extract the bucket name and object key
            string bucketName = urlParts[0].Split('.')[0];
            string objectKey = string.Join("/", urlParts, 1, urlParts.Length - 1);

            return (bucketName, objectKey);
        }


        public async Task<bool> WasBookBought(Book book,Profile profile)
        {
            var found = await _BookieDBContext.ProfileBooks.FirstOrDefaultAsync(x => x.BookId == book.Id && x.ProfileId==profile.Id);
            if (found != null) return true;
            return false;
        }

        public async Task<int> ChargeSubscribersAndUpdateAuthor(int bookId,int chapterId)
        {
            // Get the book and author profile
            var book = await GetAsync(bookId);
            var authorProfile = await _ProfileRepository.GetAsync((
                                await _UserManager.FindByIdAsync(book.UserId)).Id);

            // Get all subscribers for the book
            var subscribers = await _ProfileRepository.GetBookSubscribers(bookId);

            int chargedUserCount = 0;
            // Iterate through the subscribers and process payments
            foreach (var subscriber in subscribers)
            {
                if (subscriber.Points < book.ChapterPrice)
                {
                    break; // Insufficient points
                }
                chargedUserCount += 1;

                var oldPB = await _ProfileRepository.GetProfileBookRecord(book.Id, subscriber.Id,true);

                if (oldPB.WasUnsubscribed)
                {
                    List<Chapter> chapters = await _ChaptersRepository.GetManyAsync(bookId);
                    List<int> chapterIds = new List<int>();
                    var BoughtChapterList = _ProfileRepository.ConvertStringToIds(oldPB.BoughtChapterList);
                    HandleBookWasSubscribed(ref chapterIds, BoughtChapterList, chapters);
                    BoughtChapterList.AddRange(chapterIds);
                    oldPB.BoughtChapterList = _ProfileRepository.ConvertIdsToString(BoughtChapterList);
                    subscriber.Points -= book.ChapterPrice*chapterIds.Count;
                    authorProfile.Points += book.ChapterPrice * chapterIds.Count;
                }
                else
                {
                    var BoughtChapterList = _ProfileRepository.ConvertStringToIds(oldPB.BoughtChapterList);
                    if (BoughtChapterList == null) { BoughtChapterList = new List<int>(); }

                    BoughtChapterList.Add(chapterId);
                    oldPB.BoughtChapterList = _ProfileRepository.ConvertIdsToString(BoughtChapterList);
                    subscriber.Points -= book.ChapterPrice;
                    authorProfile.Points += book.ChapterPrice;
                }              

                await _ProfileRepository.UpdateProfileBookRecord(oldPB);
                await _ProfileRepository.UpdateAsync(authorProfile);
            }

            // Update the author's profile
            await _ProfileRepository.UpdateAsync(authorProfile);          

            return chargedUserCount;
        }

        public void HandleBookWasSubscribed(ref List<int> chapterIds,List<int> BoughtChapterList, List<Chapter> actualChapters)
        {
            
            List<int> actualChapterIds = actualChapters.Select(x => x.Id).ToList();
            //if some chapters were bought, get their ids, else buy all chapters
            if (BoughtChapterList != null)
                chapterIds = actualChapterIds.Where(x => !BoughtChapterList.Contains(x)).ToList();
            else
                chapterIds = actualChapterIds;

        }

    }
}
