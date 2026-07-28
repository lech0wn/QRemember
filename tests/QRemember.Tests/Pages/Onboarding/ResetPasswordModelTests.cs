using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;

namespace QRemember.Tests.Pages.Onboarding;

public class ResetPasswordModelTests
{
    private static (ResetPasswordModel Model, Mock<UserManager<ApplicationUser>> UserManager) CreateModel()
    {
        var userManager = IdentityMockFactory.MockUserManager();
        var model = new ResetPasswordModel(userManager.Object)
        {
            Email = "jamie@example.com",
            Code = "123456",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };
        PageModelTestHelpers.Bind(model);
        return (model, userManager);
    }

    [Fact]
    public void OnGet_SetsEmail_FromQueryParameter()
    {
        var (model, _) = CreateModel();

        model.OnGet("someone@example.com");

        Assert.Equal("someone@example.com", model.Email);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelStateInvalid()
    {
        var (model, userManager) = CreateModel();
        model.ModelState.AddModelError("Code", "The code must be 6 digits");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        userManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_AddsError_WhenUserNotFound()
    {
        var (model, userManager) = CreateModel();
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Contains("invalid or has expired", error.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_AddsError_WhenCodeIsInvalid()
    {
        var (model, userManager) = CreateModel();
        var user = new ApplicationUser { Email = model.Email };
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, model.Code))
            .ReturnsAsync(false);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Contains("invalid or has expired", error.ErrorMessage);
        userManager.Verify(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_RedirectsToLoginWithPasswordResetFlag_WhenResetSucceeds()
    {
        var (model, userManager) = CreateModel();
        var user = new ApplicationUser { Email = model.Email };
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, model.Code))
            .ReturnsAsync(true);
        userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token", model.Password))
            .ReturnsAsync(IdentityResult.Success);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Login", redirect.PageName);
        Assert.Equal(true, redirect.RouteValues!["passwordReset"]);
    }

    [Fact]
    public async Task OnPostAsync_AddsIdentityErrors_WhenResetFails()
    {
        var (model, userManager) = CreateModel();
        var user = new ApplicationUser { Email = model.Email };
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, model.Code))
            .ReturnsAsync(true);
        userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token", model.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Token expired." }));

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Equal("Token expired.", error.ErrorMessage);
    }
}
