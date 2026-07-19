using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRemember.Web.Models;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public required string Email { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Registered { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, Password, isPersistent: true, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToPage("/Index");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked out. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }

        return Page();
    }
}
