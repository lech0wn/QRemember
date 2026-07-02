using System.ComponentModel.DataAnnotations;

namespace QRemember.Web.Models;

public class Photo
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required]
    public string CloudinaryUrl { get; set; } = string.Empty;

    [Required]
    public string CloudinaryPublicId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? UploaderName { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsApproved { get; set; } = true;

    public bool IsHidden { get; set; } = false;
}