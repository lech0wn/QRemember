using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;

namespace QRemember.Web.Pages;

[AllowAnonymous]
public class GuestUploadModel : PageModel
{
    private readonly AppDbContext _db;

    public GuestUploadModel(AppDbContext db)
    {
        _db = db;
    }

    public int MaxPhotos { get; } = 15;
    public string? EventCode { get; private set; }
    public string? EventName { get; private set; }
    public string? EventHashtag { get; private set; }
    public int? EventId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool EventFound { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? code = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            // No code provided - show QR scanner mode
            EventFound = false;
            return Page();
        }

        // Code provided - try to find the event
        var eventEntity = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventCode == code && e.IsActive);

        if (eventEntity is null)
        {
            ErrorMessage = "Event not found or no longer active.";
            EventFound = false;
            return Page();
        }

        if (eventEntity.ExpiresAt <= DateTime.UtcNow)
        {
            ErrorMessage = "This event has expired.";
            EventFound = false;
            return Page();
        }

        // Event found - show upload mode
        EventFound = true;
        EventCode = eventEntity.EventCode;
        EventName = eventEntity.Name;
        EventHashtag = eventEntity.EventCode;
        EventId = eventEntity.Id;

        return Page();
    }

    // Handle QR code scanning - find event by code
    public async Task<IActionResult> OnPostScanAsync([FromBody] ScanRequest request)
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

        var eventEntity = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == eventCode && e.IsActive);

        if (eventEntity is null)
        {
            return new JsonResult(new ScanResponse
            {
                Success = false,
                Message = "Event not found. Please check the QR code."
            });
        }

        if (eventEntity.ExpiresAt <= DateTime.UtcNow)
        {
            return new JsonResult(new ScanResponse
            {
                Success = false,
                Message = "This event has expired."
            });
        }

        // Redirect to the same page with the event code
        return new JsonResult(new ScanResponse
        {
            Success = true,
            RedirectUrl = Url.Page("/GuestUpload", new { code = eventCode })
        });
    }

    // Handle manual event code entry
    public async Task<IActionResult> OnPostLookupAsync([FromBody] LookupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventCode))
        {
            return new JsonResult(new { success = false, message = "Please enter an event code." });
        }

        var eventCode = request.EventCode.Trim();

        var eventEntity = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == eventCode && e.IsActive);

        if (eventEntity is null)
        {
            return new JsonResult(new { success = false, message = "Event not found." });
        }

        if (eventEntity.ExpiresAt <= DateTime.UtcNow)
        {
            return new JsonResult(new { success = false, message = "This event has expired." });
        }

        return new JsonResult(new
        {
            success = true,
            redirectUrl = Url.Page("/GuestUpload", new { code = eventCode })
        });
    }

    // Handle photo upload
    public async Task<IActionResult> OnPostUploadAsync([FromBody] UploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventCode))
        {
            return new JsonResult(new { success = false, message = "Event code is required." });
        }

        var eventEntity = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == request.EventCode && e.IsActive);

        if (eventEntity is null)
        {
            return new JsonResult(new { success = false, message = "Event not found." });
        }

        if (request.PhotoData == null || request.PhotoData.Count == 0)
        {
            return new JsonResult(new { success = false, message = "No photos to upload." });
        }

        try
        {
            // TODO: Upload photos to Cloudinary or other service
            // For each photo in request.PhotoData:
            // 1. Upload to Cloudinary
            // 2. Save to database with eventId = eventEntity.Id
            // 3. Set uploader name = request.GuestName

            // Simulate successful upload
            await Task.Delay(500);

            return new JsonResult(new
            {
                success = true,
                message = $"{request.PhotoData.Count} photo(s) uploaded successfully!",
                photoCount = request.PhotoData.Count
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                success = false,
                message = $"Upload failed: {ex.Message}"
            });
        }
    }

    public class ScanRequest
    {
        public string QrData { get; set; } = string.Empty;
    }

    public class LookupRequest
    {
        public string EventCode { get; set; } = string.Empty;
    }

    public class UploadRequest
    {
        public string EventCode { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public List<string> PhotoData { get; set; } = new List<string>();
    }

    public class ScanResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? RedirectUrl { get; set; }
    }
}