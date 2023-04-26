using Bakalauras.Auth.Model;
using Bakalauras.data.repositories;
using Bakalauras.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bakalauras.data.dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Data;

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
        private readonly RoleManager<IdentityRole> _RoleManager;
        public AdminController(BookieDBContext context, UserManager<BookieUser> mng, IBookRepository bookRepository,
            IAdminRepository adrep, RoleManager<IdentityRole> roleManager)
        {
            _BookieDBContext = context;
            _UserManager = mng;
            _BookRepository = bookRepository;
            _AdminRepository = adrep;
            _RoleManager = roleManager;
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

        [HttpPost]
        [Authorize(Roles = BookieRoles.BookieUser + "," + BookieRoles.Admin)]
        public async Task<IActionResult> AssignRoleToUser(SetRoleDto dto)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roleExists = await _RoleManager.RoleExistsAsync(dto.roleName);
            if (!roleExists)
            {
                return BadRequest("Role does not exist");
            }

            var result = await _UserManager.AddToRoleAsync(user, dto.roleName);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Errors);
        }
    }
}
