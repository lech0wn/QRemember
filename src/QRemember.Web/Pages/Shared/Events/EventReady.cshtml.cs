using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;
using QRemember.Web.Services;

public class EventReadyModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IQrCodeService _qrCodeService;

    public EventReadyModel(AppDbContext db, UserManager<ApplicationUser> userManager, IQrCodeService qrCodeService)
    {
        _db = db;
        _userManager = userManager;
        _qrCodeService = qrCodeService;
    }

    public string EventName { get; private set; } = string.Empty;
    public string Hashtag { get; private set; } = string.Empty;
    public string EventLink { get; private set; } = string.Empty;
    public string QrCodeDataUri { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToPage("CreateEvent");
        }

        var organizerId = _userManager.GetUserId(User);
        var organizerEvent = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == code && e.OrganizerId == organizerId);

        if (organizerEvent is null || organizerEvent.QrCodeUrl is null)
        {
            return RedirectToPage("CreateEvent");
        }

        EventName = organizerEvent.Name;

        var slug = new string(EventName.Where(char.IsLetterOrDigit).ToArray());
        Hashtag = "#" + (slug.Length > 0 ? slug : "YourEvent");

        EventLink = organizerEvent.QrCodeUrl;
        QrCodeDataUri = _qrCodeService.GeneratePngDataUri(EventLink);
        ExpiresAt = organizerEvent.ExpiresAt;

        return Page();
    }
}
