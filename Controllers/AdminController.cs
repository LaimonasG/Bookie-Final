using Bakalauras.Auth.Model;
using Bakalauras.data.repositories;
using Bakalauras.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bakalauras.data.dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController:ControllerBase
    {
        private readonly BookieDBContext _BookieDBContext;
        private readonly IBookRepository _BookRepository;
        private readonly IAdminRepository _AdminRepository;
        private readonly UserManager<BookieUser> _UserManager;
        public AdminController(BookieDBContext context, UserManager<BookieUser> mng, IBookRepository bookRepository,
            IAdminRepository adrep)
        {
            _BookieDBContext = context;
            _UserManager = mng;
            _BookRepository = bookRepository;
            _AdminRepository = adrep;
        }

        [HttpGet]
        public async Task<ActionResult<List<BookieUser>>> GetUsers()
        {
            var users = await _AdminRepository.GetUserList();
            return users;
        }

        [HttpPut]
        public async Task BlockUser(string userId)
        {
            await _AdminRepository.BlockUser(userId);
        }
    }
}
