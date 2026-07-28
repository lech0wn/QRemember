using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRemember.Web.Models;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public required string Email { get; set; } = string.Empty;

    public void OnGet(string? email)
    {
        Email = email ?? string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Email);
        if (user is not null && user.Email is not null)
        {
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            var greetingName = string.IsNullOrWhiteSpace(user.DisplayName) ? "there" : WebUtility.HtmlEncode(user.DisplayName);

            var html = $"""
                <p>Hi {greetingName},</p>
                <p>Here's your QRemember password reset code:</p>
                <p style="font-size:32px; font-weight:700; letter-spacing:6px;">{code}</p>
                <p>This code expires in a few minutes. If you didn't request a password reset, you can safely ignore this email.</p>
                """;

            await _emailSender.SendEmailAsync(user.Email, "Your QRemember password reset code", html);
        }

        // Always move on to the code-entry step, whether or not an account exists for
        // this email, so this page can't be used to enumerate registered addresses.
        return RedirectToPage("ResetPassword", new { email = Email });
    }
}
