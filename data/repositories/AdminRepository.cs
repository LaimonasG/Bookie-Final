using Bakalauras.Auth.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.data.repositories
{
    public interface IAdminRepository
    {
        Task<List<BookieUser>> GetUserList();
        Task BlockUser(string userId);
    }
    public class AdminRepository : IAdminRepository
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IBookRepository _BookRepository;
        private readonly UserManager<BookieUser> _UserManager;
        public AdminRepository(BookieDBContext context, UserManager<BookieUser> mng, IBookRepository bookRepository)
        {
            _BookieDBContext = context;
            _UserManager = mng;
            _BookRepository = bookRepository;
        }

        public async Task<List<BookieUser>> GetUserList()
        {
           return await _BookieDBContext.Users.ToListAsync();
        }

        public async Task BlockUser(string userId)
        {
            var user = await _BookieDBContext.Users.Where(x => x.Id == userId).FirstOrDefaultAsync();
            user.isBlocked = true;

             _BookieDBContext.Users.Update(user);
            await _BookieDBContext.SaveChangesAsync();
        }
    }
}
