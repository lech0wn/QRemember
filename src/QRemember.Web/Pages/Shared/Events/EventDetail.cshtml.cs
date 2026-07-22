using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;

public class EventDetailModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EventDetailModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Frontend-only sample shape until photo uploads/moderation are actually persisted.
    public record SamplePhoto(int Id, string GradientFrom, string GradientTo, int Height, string Status);

    public string EventName { get; private set; } = string.Empty;
    public string Hashtag { get; private set; } = string.Empty;
    public string EventCode { get; private set; } = string.Empty;

    public List<SamplePhoto> Photos { get; } = new();

    public async Task<IActionResult> OnGetAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToPage("MyEvents");
        }

        var organizerId = _userManager.GetUserId(User);
        var organizerEvent = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == code && e.OrganizerId == organizerId);

        if (organizerEvent is null)
        {
            return RedirectToPage("MyEvents");
        }

        EventCode = organizerEvent.EventCode;
        EventName = organizerEvent.Name;

        var slug = new string(EventName.Where(char.IsLetterOrDigit).ToArray());
        Hashtag = "#" + (slug.Length > 0 ? slug : "YourEvent");

        return Page();
    }
}
