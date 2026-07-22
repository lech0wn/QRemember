using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Services;

public class PhotoUploadModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICloudinaryImageService _cloudinary;

    public PhotoUploadModel(AppDbContext db, ICloudinaryImageService cloudinary)
    {
        _db = db;
        _cloudinary = cloudinary;
    }

    public string EventName { get; private set; } = string.Empty;
    public string EventCode { get; private set; } = string.Empty;

    [BindProperty]
    public UploadInput Input { get; set; } = new();

    public class UploadInput
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Your name")]
        public string UploaderName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Caption")]
        public string? Caption { get; set; }

        [Required]
        [Display(Name = "Photo")]
        public IFormFile? PhotoFile { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string code, CancellationToken cancellationToken)
    {
        var eventEntity = await LoadEventAsync(code, cancellationToken);
        if (eventEntity is null)
        {
            return NotFound();
        }

        EventName = eventEntity.Name;
        EventCode = eventEntity.EventCode;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string code, CancellationToken cancellationToken)
    {
        var eventEntity = await LoadEventAsync(code, cancellationToken);
        if (eventEntity is null)
        {
            return NotFound();
        }

        EventName = eventEntity.Name;
        EventCode = eventEntity.EventCode;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.PhotoFile is null || Input.PhotoFile.Length == 0)
        {
            ModelState.AddModelError("Input.PhotoFile", "Please choose a photo to upload.");
            return Page();
        }

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif",
        };

        if (!allowedContentTypes.Contains(Input.PhotoFile.ContentType))
        {
            ModelState.AddModelError("Input.PhotoFile", "Only JPEG, PNG, WebP, and GIF images are allowed.");
            return Page();
        }

        await using var stream = Input.PhotoFile.OpenReadStream();

        string url;
        string publicId;
        try
        {
            (url, publicId) = await _cloudinary.UploadEventPhotoAsync(
                stream,
                Input.PhotoFile.FileName,
                eventEntity.Id,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        var caption = string.IsNullOrWhiteSpace(Input.Caption) ? null : Input.Caption.Trim();

        _db.Photos.Add(new QRemember.Web.Models.Photo
        {
            EventId = eventEntity.Id,
            CloudinaryUrl = url,
            CloudinaryPublicId = publicId,
            UploaderName = Input.UploaderName.Trim(),
            Caption = caption,
            UploadedAt = DateTime.UtcNow,
            IsApproved = true,
            IsHidden = false,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return RedirectToPage("/Shared/Gallery/EventGallery", new { code = EventCode });
    }

    private async Task<QRemember.Web.Models.Event?> LoadEventAsync(string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventCode == normalizedCode && e.IsActive, cancellationToken);
    }
}
