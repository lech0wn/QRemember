using Microsoft.AspNetCore.Mvc;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;

namespace QRemember.Tests.Pages.Onboarding;

public class LogoutModelTests
{
    private static (LogoutModel Model, Mock<Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser>> SignInManager) CreateModel()
    {
        var userManager = IdentityMockFactory.MockUserManager();
        var signInManager = IdentityMockFactory.MockSignInManager(userManager.Object);
        var model = new LogoutModel(signInManager.Object);
        PageModelTestHelpers.Bind(model);
        return (model, signInManager);
    }

    [Fact]
    public void OnGet_RedirectsToLogin()
    {
        var (model, _) = CreateModel();

        var result = model.OnGet();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Shared/Onboarding/Login", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_SignsOut_AndRedirectsToLogin()
    {
        var (model, signInManager) = CreateModel();

        var result = await model.OnPostAsync();

        signInManager.Verify(s => s.SignOutAsync(), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Shared/Onboarding/Login", redirect.PageName);
    }
}
