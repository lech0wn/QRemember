using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;
using QRemember.Web.Services;
using QRemember.Web.Models.ViewModels;
using System.Text.RegularExpressions;


namespace QRemember.Web.Pages.Guest;

[AllowAnonymous]
public class GuestUploadModel : PageModel
{
    private readonly AppDbContext _dbContext;
    private readonly ICloudinaryImageService _cloudinary;
    private readonly ILogger<GuestUploadModel> _logger;

    public GuestUploadModel(AppDbContext dbContext, ICloudinaryImageService cloudinary ,ILogger<GuestUploadModel> logger)
    {
        _dbContext = dbContext;
        _cloudinary = cloudinary;
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


    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };


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

        return JsonSuccess(Url.Page("/Guest/GuestUpload", new { code = eventCode }));
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

        return JsonSuccess(Url.Page("/Guest/GuestUpload", new { code = eventCode }));
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

        if (IsEventExpired(eventEntity))
        {
            return JsonError("This event has expired.");
        }

        if (request.PhotoData == null || request.PhotoData.Count == 0)
        {
            return JsonError("No photos to upload.");
        }

        var existingCount = await _dbContext.Photos.CountAsync(p => p.EventId == eventEntity.Id);
        if(existingCount + request.PhotoData.Count > MaxPhotosLimit)
        {
            return JsonError($"This event can only hold {MaxPhotosLimit} photos.");
        }
        try
        {
            var savedCount = await ProcessPhotoUploadsAsync (eventEntity, request);

            return new JsonResult(new
            {
                success = true,
                message = $"{savedCount} photo(s) uploaded and awaiting approval",
                photoCount = savedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo upload failed for event {EventCode}", request.EventCode);
            return JsonError("Upload failed. Please try again.");
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


    private IActionResult JsonSuccess(string? redirectUrl)
    {
        return new JsonResult(new { success = true, redirectUrl });
    }


    private async Task<int> ProcessPhotoUploadsAsync(Event eventEntity, UploadRequest request)
    {
        var guestName = string.IsNullOrWhiteSpace(request.GuestName) ? "Anonymous" : request.GuestName.Trim();
        var saved = 0;

        foreach (var PhotoDataUrl in request.PhotoData)
        {
            if (!TryDecodeDataUrl(PhotoDataUrl, out var contentType, out var bytes))
            {
                _logger.LogWarning("Skipped an unparseable photo payload for event {EventCode}", request.EventCode);
                continue;
            }

            if (!AllowedContentTypes.Contains(contentType))
            {
                _logger.LogWarning("Skipped disallowed content type {ContentType} for event {EventCode}", contentType, request.EventCode);
                continue;
            }

            await using var stream = new MemoryStream(bytes);
            var fileName = $"guest-{Guid.NewGuid():N}.{GetExtension(contentType)}";

            var (url, publicId) = await _cloudinary.UploadEventPhotoAsync(stream, fileName, eventEntity.Id, CancellationToken.None);

            _dbContext.Photos.Add(new Photo
            {
                EventId = eventEntity.Id,
                CloudinaryUrl = url,
                CloudinaryPublicId = publicId,
                UploaderName = guestName,
                Caption = null,
                UploadedAt = DateTime.UtcNow,
                IsApproved = false,
                IsHidden = false,
            });

            saved++;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Saved {Count} pending photos for event {EventCode}", saved, request.EventCode);
        return saved;
    }


    private static bool TryDecodeDataUrl(string DataUrl, out string contentType, out byte[] bytes)
    {
        contentType = string.Empty;
        bytes = Array.Empty<byte>();

        var match = Regex.Match(DataUrl, @"^data:(?<type>[\w/+-]+);base64,(?<data>.+)$", RegexOptions.Singleline);
        if (!match.Success)
        {
            return false;
        }

        try
        {
            contentType = match.Groups["type"].Value;
            bytes = Convert.FromBase64String(match.Groups["data"].Value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }


    private static string GetExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => "png",
        "image/webp" => "webp",
        "image/gif" => "gif",
        _ => "jpg",
    };


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


    public class SetStatusRequest
    {
        public int PhotoId { get; set; }
        public string Status { get; set; } = "";
    }


    public class DeletePhotoRequest
    {
        public int PhotoId { get; set; }
    }
}