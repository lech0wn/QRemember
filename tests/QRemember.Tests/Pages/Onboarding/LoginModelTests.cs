using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace QRemember.Tests.Pages.Onboarding;

public class LoginModelTests
{
    private static (LoginModel Model, Mock<UserManager<ApplicationUser>> UserManager, Mock<SignInManager<ApplicationUser>> SignInManager) CreateModel()
    {
        var userManager = IdentityMockFactory.MockUserManager();
        var signInManager = IdentityMockFactory.MockSignInManager(userManager.Object);
        var model = new LoginModel(signInManager.Object, userManager.Object)
        {
            Email = "user@example.com",
            Password = "password123"
        };
        PageModelTestHelpers.Bind(model);
        return (model, userManager, signInManager);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelStateInvalid()
    {
        var (model, _, _) = CreateModel();
        model.ModelState.AddModelError("Password", "Password is required");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsError_WhenUserNotFound()
    {
        var (model, userManager, _) = CreateModel();
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_RedirectsToCreateEvent_WhenSignInSucceeds()
    {
        var (model, userManager, signInManager) = CreateModel();
        var user = new ApplicationUser { Email = model.Email };
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        signInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, true, true))
            .ReturnsAsync(SignInResult.Success);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Shared/Events/CreateEvent", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_AddsLockoutError_WhenAccountIsLockedOut()
    {
        var (model, userManager, signInManager) = CreateModel();
        var user = new ApplicationUser { Email = model.Email };
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        signInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, true, true))
            .ReturnsAsync(SignInResult.LockedOut);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Contains("locked out", error.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_AddsInvalidCredentialsError_WhenPasswordIncorrect()
    {
        var (model, userManager, signInManager) = CreateModel();
        var user = new ApplicationUser { Email = model.Email };
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        signInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, true, true))
            .ReturnsAsync(SignInResult.Failed);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Contains("Invalid email or password", error.ErrorMessage);
    }
}
