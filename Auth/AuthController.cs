using Bakalauras.Auth.Model;
using Bakalauras.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bakalauras.data.repositories;
using Bakalauras.data;
using Bakalauras.data.entities;

namespace Bookie.controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<BookieUser> _UserManager;
        private readonly Bakalauras.Auth.IJwtTokenService _JwtTokenService;
        private readonly IProfileRepository _ProfileRepo;

        public AuthController(UserManager<BookieUser> userManager, IJwtTokenService jwtTokenService)
        {
            _UserManager = userManager;
            _JwtTokenService = jwtTokenService;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerUserDto)
        {
            var user = await _UserManager.FindByNameAsync(registerUserDto.UserName);
            if (user != null)
                return BadRequest("Request invalid.");

            var newUser = new BookieUser
            {
                Email = registerUserDto.Email,
                UserName = registerUserDto.UserName
            };
            var createUserResult = await _UserManager.CreateAsync(newUser, registerUserDto.Password);
            if (!createUserResult.Succeeded)
                return BadRequest("Could not create a user.");

           // Profile profile = new Profile(newUser);
           //await profileRepo.CreateAsync(profile);

            await _UserManager.AddToRoleAsync(newUser, BookieRoles.BookieUser);

            return CreatedAtAction(nameof(Register), new UserDto(newUser.Id, newUser.UserName, newUser.Email));
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult> Login(LoginDto loginDto)
        {
            var user = await _UserManager.FindByNameAsync(loginDto.UserName);
            if (user == null)
                return BadRequest("User name or password is invalid.");

            var isPasswordValid = await _UserManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
                return BadRequest("User name or password is invalid.");

            // valid user
            var roles = await _UserManager.GetRolesAsync(user);
            var accessToken =  _JwtTokenService.CreateAccessToken(user.UserName,user.Id,roles);

            return Ok(new SuccessfulLoginDto(accessToken));
        }
    }
}
