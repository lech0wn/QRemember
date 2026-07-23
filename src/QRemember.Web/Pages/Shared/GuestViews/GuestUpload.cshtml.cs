using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QRemember.Web.Pages
{
    public class GuestUploadModel : PageModel
    {
        // TODO: inject your DbContext / event lookup service here, e.g.:
        // private readonly ApplicationDbContext _db;
        // public GuestUploadModel(ApplicationDbContext db) => _db = db;

        public void OnGet()
        {
        }

        /// <summary>
        /// Called via fetch() from guest-upload.js once a QR code has been decoded
        /// client-side (either from an uploaded/pasted image or a live camera scan).
        /// Validates the decoded value against a real event and returns the gallery
        /// URL to redirect the guest to.
        /// </summary>
        public IActionResult OnPostScan([FromBody] ScanRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.QrData))
            {
                return new JsonResult(new ScanResponse
                {
                    Success = false,
                    Message = "That QR code couldn't be read. Please try again."
                });
            }

            // TODO: replace this stub with a real lookup, e.g.:
            // var eventCode = ParseEventCode(request.QrData);
            // var eventEntity = _db.Events.FirstOrDefault(e => e.Code == eventCode && e.IsActive);
            // if (eventEntity is null) { return not-found response }

            var eventCode = request.QrData.Trim();

            if (string.IsNullOrEmpty(eventCode))
            {
                return new JsonResult(new ScanResponse
                {
                    Success = false,
                    Message = "This doesn't look like a valid event QR code."
                });
            }

            return new JsonResult(new ScanResponse
            {
                Success = true,
                RedirectUrl = Url.Page("/Gallery", new { eventCode })
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
