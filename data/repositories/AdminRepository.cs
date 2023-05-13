using Bakalauras.Auth.Model;
using Bakalauras.data.dtos;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.data.repositories
{
    public interface IAdminRepository
    {
        Task<List<UserAdminPageDto>> GetUserList();
        Task<bool> IsBlocked(string userId);
    }
    public class AdminRepository : IAdminRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IProfileRepository _ProfileRepository;
        public AdminRepository(BookieDBContext context, IProfileRepository repo)
        {
            _BookieDBContext = context;
            _ProfileRepository = repo;
        }

        public async Task<List<UserAdminPageDto>> GetUserList()
        {
            var adminRoleId = await _BookieDBContext.Roles
                            .Where(r => r.Name == "Admin")
                            .Select(r => r.Id)
                            .FirstOrDefaultAsync();

            List<BookieUser> users;
            if (adminRoleId != null)
            {
                var userIds = await _BookieDBContext.UserRoles.Where(x => x.RoleId == adminRoleId).Select(x => x.UserId)
           .ToListAsync();
                users = await _BookieDBContext.Users.Where(x => !userIds.Contains(x.Id)).ToListAsync();
            }
            else
            {
                users = await _BookieDBContext.Users.ToListAsync();
            }

            List<UserAdminPageDto> rez = new List<UserAdminPageDto>();
            foreach (var user in users)
            {
                var profile = await _ProfileRepository.GetAsync(user.Id);
                UserAdminPageDto temp = new UserAdminPageDto(user.Id, user.UserName, user.Email, user.isBlocked ? 1 : 0, profile.Points);
                rez.Add(temp);
            }
            return rez;
        }

        public async Task<bool> IsBlocked(string userId)
        {
            return await _BookieDBContext.Users.Where(x => x.Id == userId).Select(y => y.isBlocked).FirstOrDefaultAsync();
        }

    }
}
