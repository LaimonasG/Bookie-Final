using Microsoft.AspNetCore.Mvc;
using Bakalauras.Auth.Model;
using Bakalauras.Auth;
using Bakalauras.data.dtos;
using Bakalauras.data.entities;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace Bakalauras.Controllers
{
    [ApiController]
    [Route("api/writer")]
    public class WriterPlatformController : ControllerBase
    {
        private readonly IProfileRepository _ProfileRepository;
        private readonly IBookRepository _BookRepository;
        private readonly IAuthorizationService _AuthorizationService;
        private readonly UserManager<BookieUser> _UserManager;
        private readonly IWriterPlatformRepository _WriterPlatformRepository;


        public WriterPlatformController(IProfileRepository repo, IAuthorizationService authService,
            UserManager<BookieUser> userManager, IBookRepository repob,IWriterPlatformRepository writrepo)
        {
            _ProfileRepository = repo;
            _AuthorizationService = authService;
            _UserManager = userManager;
            _BookRepository = repob;
            _WriterPlatformRepository = writrepo;
        }

        [HttpGet]
        [Route("sales")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<WriterSalesData>> GetWriterSales()
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));

            WriterSalesData result = new WriterSalesData
                (
                await _WriterPlatformRepository.GetBookData(user.Id),
                await _WriterPlatformRepository.GetTextData(user.Id)
                );

            return Ok(result);
        }

        [HttpGet]
        [Route("getPayment")]
        [Authorize(Roles = BookieRoles.BookieReader + "," + BookieRoles.Admin)]
        public async Task<ActionResult<WriterPaymentConfirmation>> GetWriterPayment(double cashOutAmount)
        {
            var user = await _UserManager.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub));

            var response = await _WriterPlatformRepository.ProcessWriterPayment(user.Id, cashOutAmount);

            if (response.WithrawalTooSmall)
            {
                return BadRequest(string.Format("Išgryninimo suma (%1 Eur) per maža.", response.EurAmount));
            }

            if (!response.Confirmed)
            {
                return BadRequest("Įvyko banko klaida, bandykite iš naujo.");
            }

            return Ok(response);
        }
    }
}
