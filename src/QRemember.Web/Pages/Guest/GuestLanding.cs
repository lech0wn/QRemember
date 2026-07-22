using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRemember.Web.Services;

public class IndexModel : PageModel
{
    private readonly IEventLookupService _lookup;
    public IndexModel(IEventLookupService lookup) => _lookup = lookup;

    [BindProperty]
    public string? EventCodeInput { get; set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var eventEntity = await _lookup.GetActiveEventByCodeAsync(EventCodeInput, ct);
        if (eventEntity is null)
        {
            ErrorMessage = "That event code wasn't found. Double-check and try again.";
            return Page();
        }
        return RedirectToPage("/Shared/Gallery/EventGallery", new { code = eventEntity.EventCode });
    }
}