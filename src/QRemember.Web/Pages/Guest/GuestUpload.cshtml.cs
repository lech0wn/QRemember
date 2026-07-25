using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;

namespace QRemember.Web.Pages;

[AllowAnonymous]
public class GuestUploadModel : PageModel
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<GuestUploadModel> _logger;

    public GuestUploadModel(AppDbContext dbContext, ILogger<GuestUploadModel> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public const int MaxPhotosLimit = 15;
    
    public int MaxPhotos => MaxPhotosLimit;
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
            EventFound = false;
            return Page();
        }

        var eventEntity = await FindActiveEventAsync(code.Trim());
        
        if (eventEntity is null)
        {
            ErrorMessage = "Event not found or no longer active.";
            EventFound = false;
            return Page();
        }

        if (IsEventExpired(eventEntity))
        {
            ErrorMessage = "This event has expired.";
            EventFound = false;
            return Page();
        }

        LoadEventData(eventEntity);
        return Page();
    }

    public async Task<IActionResult> OnPostScanAsync([FromBody] ScanRequest request)
    {
        if (!IsValidScanRequest(request))
        {
            return JsonError("That QR code couldn't be read. Please try again.");
        }

        var eventCode = request!.QrData.Trim();
        var eventEntity = await FindActiveEventAsync(eventCode);

        if (eventEntity is null)
        {
            return JsonError("Event not found. Please check the QR code.");
        }

        if (IsEventExpired(eventEntity))
        {
            return JsonError("This event has expired.");
        }

        return JsonSuccess(Url.Page("/GuestUpload", new { code = eventCode }));
    }

    public async Task<IActionResult> OnPostLookupAsync([FromBody] LookupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.EventCode))
        {
            return JsonError("Please enter an event code.");
        }

        var eventCode = request.EventCode.Trim();
        var eventEntity = await FindActiveEventAsync(eventCode);

        if (eventEntity is null)
        {
            return JsonError("Event not found.");
        }

        if (IsEventExpired(eventEntity))
        {
            return JsonError("This event has expired.");
        }

        return JsonSuccess(Url.Page("/GuestUpload", new { code = eventCode }));
    }

    public async Task<IActionResult> OnPostUploadAsync([FromBody] UploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.EventCode))
        {
            return JsonError("Event code is required.");
        }

        var eventEntity = await FindActiveEventAsync(request.EventCode.Trim());
        
        if (eventEntity is null)
        {
            return JsonError("Event not found.");
        }

        if (request.PhotoData == null || request.PhotoData.Count == 0)
        {
            return JsonError("No photos to upload.");
        }

        try
        {
            await ProcessPhotoUploadsAsync(eventEntity, request);
            
            return new JsonResult(new
            {
                success = true,
                message = $"{request.PhotoData.Count} photo(s) uploaded successfully!",
                photoCount = request.PhotoData.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo upload failed for event {EventCode}", request.EventCode);
            return JsonError($"Upload failed: {ex.Message}");
        }
    }

    private async Task<Event?> FindActiveEventAsync(string code)
    {
        return await _dbContext.Events
            .FirstOrDefaultAsync(e => e.EventCode == code && e.IsActive);
    }

    private bool IsEventExpired(Event eventEntity)
    {
        return eventEntity.ExpiresAt <= DateTime.UtcNow;
    }

    private void LoadEventData(Event eventEntity)
    {
        EventFound = true;
        EventCode = eventEntity.EventCode;
        EventName = eventEntity.Name;
        EventHashtag = eventEntity.EventCode;
        EventId = eventEntity.Id;
    }

    private bool IsValidScanRequest(ScanRequest? request)
    {
        return request is not null && !string.IsNullOrWhiteSpace(request.QrData);
    }

    private IActionResult JsonError(string message)
    {
        return new JsonResult(new { success = false, message });
    }

    private IActionResult JsonSuccess(string redirectUrl)
    {
        return new JsonResult(new { success = true, redirectUrl });
    }

    private async Task ProcessPhotoUploadsAsync(Event eventEntity, UploadRequest request)
    {
        // TODO: Implement actual photo upload logic
        // 1. Upload each photo to Cloudinary
        // 2. Save photo records to database
        // 3. Associate with eventEntity.Id
        
        await Task.Delay(500); // Simulate processing
        _logger.LogInformation("Processing {Count} photos for event {EventCode}", 
            request.PhotoData.Count, request.EventCode);
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
}