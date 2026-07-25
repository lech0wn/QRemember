using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRemember.Web.Services;

namespace QRemember.Web.Pages.Guest;  // FIX: Changed from Pages to Pages.Guest

public class GuestLandingModel : PageModel
{
    private readonly IEventLookupService _lookup;

    public GuestLandingModel(IEventLookupService lookup)
    {
        _lookup = lookup;
    }

    [BindProperty]
    public string? EventCodeInput { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(EventCodeInput))
        {
            ErrorMessage = "Please enter an event code.";
            return Page();
        }

        var eventEntity = await _lookup.GetActiveEventByCodeAsync(EventCodeInput, ct);
        if (eventEntity is null)
        {
            ErrorMessage = "That event code wasn't found. Double-check and try again.";
            return Page();
        }

        return RedirectToPage("/Guest/GuestEventGallery", new { code = eventEntity.EventCode });
    }
}