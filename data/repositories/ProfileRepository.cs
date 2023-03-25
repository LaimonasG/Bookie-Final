using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
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
    }
    public class ProfileRepository : IProfileRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly UserManager<BookieUser> _UserManager;
        public ProfileRepository(BookieDBContext context,UserManager<BookieUser> mng)
        {
            _BookieDBContext = context;
            _UserManager = mng;
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
    }
}
