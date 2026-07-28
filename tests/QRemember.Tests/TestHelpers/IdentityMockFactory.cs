using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using QRemember.Web.Models;

namespace QRemember.Tests.TestHelpers;

// UserManager/SignInManager have no parameterless constructors, so tests must
// supply the full dependency list (mostly nulls) to get a mockable instance.
public static class IdentityMockFactory
{
    public static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    public static Mock<SignInManager<ApplicationUser>> MockSignInManager(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(
            userManager, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }
}
