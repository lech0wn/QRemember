using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;

public class MyEventsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    // Cycled purely for card visuals until event cover photos exist.
    private static readonly (string From, string To)[] Gradients =
    {
        ("#4A4139", "#1C1C1C"),
        ("#2E3238", "#111417"),
        ("#41362A", "#1C1C1C"),
        ("#1F4A4D", "#12262A"),
    };

    public MyEventsModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public record EventCardViewModel(string EventCode, string Title, int Year, string GradientFrom, string GradientTo, int PhotoHeight, DateTime ExpiresAtUtc);

    public List<EventCardViewModel> Events { get; } = new();

    public async Task OnGetAsync()
    {
        var organizerId = _userManager.GetUserId(User);

        var events = await _db.Events
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        for (var i = 0; i < events.Count; i++)
        {
            var (from, to) = Gradients[i % Gradients.Length];
            Events.Add(new EventCardViewModel(events[i].EventCode, events[i].Name, events[i].EventDate.Year, from, to, 220, events[i].ExpiresAt));
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(string code)
    {
        var organizerId = _userManager.GetUserId(User);

        var organizerEvent = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == code && e.OrganizerId == organizerId);

        if (organizerEvent is not null)
        {
            _db.Events.Remove(organizerEvent);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
