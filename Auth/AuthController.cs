using Bakalauras.Auth.Model;
using Bakalauras.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bakalauras.data.repositories;
using Bakalauras.data;
using Bakalauras.data.entities;

namespace Bakalauras.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<BookieUser> _UserManager;
        private readonly Bakalauras.Auth.IJwtTokenService _JwtTokenService;
        private readonly IProfileRepository _ProfileRepo;

        public AuthController(UserManager<BookieUser> userManager, IJwtTokenService jwtTokenService,IProfileRepository repo)
        {
            _UserManager = userManager;
            _JwtTokenService = jwtTokenService;
            _ProfileRepo = repo;
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

            Profile profile = new Profile
            {
                UserId = newUser.Id,
                Points = 0,
                User=newUser
            };

            await _ProfileRepo.CreateAsync(profile);

            await _UserManager.AddToRoleAsync(newUser, BookieRoles.BookieUser);
            await _UserManager.AddToRoleAsync(newUser, BookieRoles.BookieReader);

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
            var refreshToken = _JwtTokenService.CreateRefreshToken();

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new SuccessfulLoginDto(accessToken));
        }

        [HttpPost]
        [Route("refresh-token")]
        [Authorize(Roles = BookieRoles.BookieUser)]
        public async Task<ActionResult> RefreshToken([FromBody] string refreshTokenDto)
        {
            var user = await _JwtTokenService.GetUserByRefreshTokenAsync(refreshTokenDto);
            if (user == null)
                return BadRequest("Invalid refresh token.");

            var roles = await _UserManager.GetRolesAsync(user);
            var accessToken = _JwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);

            var newRefreshToken = _JwtTokenService.CreateRefreshToken();

            await _JwtTokenService.UpdateRefreshTokenAsync(user, refreshTokenDto, newRefreshToken);

            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new RefreshTokenDto(accessToken));
        }

        [HttpPut]
        [Route("block")]
        [Authorize(Roles = BookieRoles.Admin)]
        public async Task<ActionResult> BlockUser(string userName)
        {
            var user = await _UserManager.FindByNameAsync(userName);

            if (user == null)
                return BadRequest("Username not found.");

            user.isBlocked= true;

            await _UserManager.UpdateAsync(user);

            return Ok(new UserBlockedDto(user.Id,user.UserName,user.isBlocked));
        }
    }
}
