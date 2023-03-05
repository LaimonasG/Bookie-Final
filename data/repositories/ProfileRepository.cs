using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Microsoft.EntityFrameworkCore;
using PagedList;

namespace Bakalauras.data.repositories
{
    public interface IProfileRepository
    {
        Task CreateAsync(Profile profile);
        Task DeleteAsync(Profile profile);
        Task<Profile?> GetAsync(int profileId);
        Task<IReadOnlyList<Profile>> GetManyAsync();
       // Task<PagedList<Profile>> GetManyAsync(ProfileSearchParams parameters);
        Task UpdateAsync(Profile profile);
    }
    public class ProfileRepository : IProfileRepository
    {
        private readonly BookieDBContext bookieDBContext;
        public ProfileRepository(BookieDBContext context)
        {
            bookieDBContext = context;
        }

        public async Task<Profile?> GetAsync(int profileId)
        {
            return await bookieDBContext.Profiles.FirstOrDefaultAsync(x => x.Id == profileId);
        }

        public async Task<IReadOnlyList<Profile>> GetManyAsync()
        {
            return await bookieDBContext.Profiles.ToListAsync();
        }

        public async Task<IReadOnlyList<Profile>> GetUserBooksAsync(string userId)
        {
            return await bookieDBContext.Profiles.Where(x => x.UserId == userId).ToListAsync();
        }

        //public async Task<PagedList<Profile>> GetManyAsync(ProfileSearchParams parameters)
        //{
        //    var queryable = bookieDBContext.Profile.AsQueryable().OrderBy(o => o.Name);

        //    return await PagedList<Profile>.CreateAsync(queryable, parameters.pageNumber,
        //        parameters.PageSize);
        //}

        public async Task CreateAsync(Profile profile)
        {
            bookieDBContext.Profiles.Add(profile);
            await bookieDBContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Profile profile)
        {
            bookieDBContext.Profiles.Update(profile);
            await bookieDBContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Profile profile)
        {
            bookieDBContext.Profiles.Remove(profile);
            await bookieDBContext.SaveChangesAsync();
        }
    }
}
