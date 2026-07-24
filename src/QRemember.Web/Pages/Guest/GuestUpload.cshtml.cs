using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QRemember.Web.Pages
{
    public class GuestUploadModel : PageModel
    {
        // TODO: inject your DbContext / event lookup service here
        // private readonly AppDbContext _db;
        // public GuestUploadModel(AppDbContext db) => _db = db;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostScan([FromBody] ScanRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.QrData))
            {
                return new JsonResult(new ScanResponse
                {
                    Success = false,
                    Message = "That QR code couldn't be read. Please try again."
                });
            }

            var eventCode = request.QrData.Trim();

            // TODO: Replace with real database lookup
            // var eventEntity = await _db.Events
            //     .FirstOrDefaultAsync(e => e.EventCode == eventCode && e.IsActive);
            // if (eventEntity is null) { ... }

            if (string.IsNullOrEmpty(eventCode))
            {
                return new JsonResult(new ScanResponse
                {
                    Success = false,
                    Message = "This doesn't look like a valid event QR code."
                });
            }

            // Fix: Redirect to the correct gallery page
            return new JsonResult(new ScanResponse
            {
                Success = true,
                RedirectUrl = Url.Page("/Guest/GuestEventGallery", new { code = eventCode })
                // Or use: RedirectUrl = $"/Guest/GuestEventGallery?code={eventCode}"
            });
        }

        public class ScanRequest
        {
            public string QrData { get; set; } = string.Empty;
        }

        public class ScanResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? RedirectUrl { get; set; }
        }
    }
}