using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;

namespace QRemember.Tests.Pages.Onboarding;

public class ForgotPasswordModelTests
{
    private static (ForgotPasswordModel Model, Mock<UserManager<ApplicationUser>> UserManager, Mock<IEmailSender> EmailSender) CreateModel()
    {
        var userManager = IdentityMockFactory.MockUserManager();
        var emailSender = new Mock<IEmailSender>();
        var model = new ForgotPasswordModel(userManager.Object, emailSender.Object) { Email = string.Empty };
        PageModelTestHelpers.Bind(model);
        return (model, userManager, emailSender);
    }

    [Fact]
    public void OnGet_SetsEmail_FromQueryParameter()
    {
        var (model, _, _) = CreateModel();

        model.OnGet("someone@example.com");

        Assert.Equal("someone@example.com", model.Email);
    }

    [Fact]
    public void OnGet_DefaultsToEmptyString_WhenNoEmailProvided()
    {
        var (model, _, _) = CreateModel();

        model.OnGet(null);

        Assert.Equal(string.Empty, model.Email);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelStateInvalid()
    {
        var (model, userManager, _) = CreateModel();
        model.Email = "not-an-email";
        model.ModelState.AddModelError("Email", "Enter a valid email address");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        userManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_SendsResetCodeEmail_AndRedirectsToResetPassword_WhenUserExists()
    {
        var (model, userManager, emailSender) = CreateModel();
        var user = new ApplicationUser { Email = "found@example.com", DisplayName = "Jamie" };
        model.Email = "found@example.com";

        userManager.Setup(m => m.FindByEmailAsync("found@example.com")).ReturnsAsync(user);
        userManager.Setup(m => m.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider))
            .ReturnsAsync("123456");

        var result = await model.OnPostAsync();

        emailSender.Verify(e => e.SendEmailAsync(
            "found@example.com",
            "Your QRemember password reset code",
            It.Is<string>(html => html.Contains("123456") && html.Contains("Jamie"))), Times.Once);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("ResetPassword", redirect.PageName);
        Assert.Equal("found@example.com", redirect.RouteValues!["email"]);
    }

    [Fact]
    public async Task OnPostAsync_DoesNotSendEmail_ButStillRedirects_WhenUserDoesNotExist()
    {
        // Prevents this page from being used to enumerate registered accounts.
        var (model, userManager, emailSender) = CreateModel();
        model.Email = "nobody@example.com";
        userManager.Setup(m => m.FindByEmailAsync("nobody@example.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await model.OnPostAsync();

        emailSender.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("ResetPassword", redirect.PageName);
        Assert.Equal("nobody@example.com", redirect.RouteValues!["email"]);
    }
}
