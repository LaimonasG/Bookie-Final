using Bakalauras.Auth.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Bakalauras.Auth;

public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement, IUserOwnedResource>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceOwnerRequirement requirement,
        IUserOwnedResource resource)
    {
        if (context.User.IsInRole(BookieRoles.Admin) ||
            context.User.FindFirstValue(JwtRegisteredClaimNames.Sub) == resource.UserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class ResourceOwnerRequirement : IAuthorizationRequirement
{

}