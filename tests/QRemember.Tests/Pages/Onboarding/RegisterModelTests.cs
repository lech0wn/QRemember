using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;

namespace QRemember.Tests.Pages.Onboarding;

public class RegisterModelTests
{
    private static (RegisterModel Model, Mock<UserManager<ApplicationUser>> UserManager) CreateModel()
    {
        var userManager = IdentityMockFactory.MockUserManager();
        var model = new RegisterModel(userManager.Object)
        {
            Name = "Jamie Doe",
            Email = "jamie@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        PageModelTestHelpers.Bind(model);
        return (model, userManager);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelStateInvalid()
    {
        var (model, userManager) = CreateModel();
        model.ModelState.AddModelError("Password", "Password must be at least 8 characters");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_AddsError_WhenEmailAlreadyRegistered()
    {
        var (model, userManager) = CreateModel();
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(new ApplicationUser { Email = model.Email });

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Contains("already exists", error.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_RedirectsToLoginWithRegisteredFlag_WhenCreationSucceeds()
    {
        var (model, userManager) = CreateModel();
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Success);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Login", redirect.PageName);
        Assert.Equal(true, redirect.RouteValues!["registered"]);
    }

    [Fact]
    public async Task OnPostAsync_CreatesUser_WithNameAsDisplayNameAndEmailAsUsername()
    {
        var (model, userManager) = CreateModel();
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? createdUser = null;
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .Callback<ApplicationUser, string>((u, _) => createdUser = u)
            .ReturnsAsync(IdentityResult.Success);

        await model.OnPostAsync();

        Assert.NotNull(createdUser);
        Assert.Equal(model.Email, createdUser!.UserName);
        Assert.Equal(model.Email, createdUser.Email);
        Assert.Equal(model.Name, createdUser.DisplayName);
    }

    [Fact]
    public async Task OnPostAsync_AddsIdentityErrors_WhenCreationFails()
    {
        var (model, userManager) = CreateModel();
        userManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Equal("Password too weak.", error.ErrorMessage);
    }
}
