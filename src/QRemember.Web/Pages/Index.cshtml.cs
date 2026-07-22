using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRemember.Web.Services;

namespace QRemember.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IEventLookupService _lookup;

    public IndexModel(IEventLookupService lookup)
    {
        _lookup = lookup;
    }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    // Called via fetch() from the client after a QR code has been decoded in-browser.
    // Accepts either a full gallery URL (what your QR codes actually encode) or a bare
    // event code, and returns where the browser should navigate next.
    public async Task<IActionResult> OnPostResolveCodeAsync([FromBody] ResolveCodeRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.DecodedText))
        {
            return new JsonResult(new { success = false, message = "No QR content was found in that image." });
        }

        var raw = request.DecodedText.Trim();

        // If the QR encodes a full URL (the expected case), extract the trailing
        // {code} route segment rather than assuming the whole string is the code.
        var candidateCode = raw;
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            candidateCode = segments.Length > 0 ? segments[^1] : raw;
        }

        var eventEntity = await _lookup.GetActiveEventByCodeAsync(candidateCode, cancellationToken);
        if (eventEntity is null)
        {
            return new JsonResult(new { success = false, message = "We couldn't find an event for that QR code." });
        }

        var redirectUrl = Url.Page("/Shared/Gallery/EventGallery", new { code = eventEntity.EventCode });
        return new JsonResult(new { success = true, redirectUrl });
    }

    public class ResolveCodeRequest
    {
        public string? DecodedText { get; set; }
    }
}
