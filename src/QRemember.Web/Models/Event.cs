using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRemember.Web.Models;

public class Event
{
    public const int LifespanDays = 15;

    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string EventCode { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // The URL encoded in the event's QR code (the guest-facing link).
    public string? QrCodeUrl { get; set; }

    public DateTime EventDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Galleries are only kept for a limited time; not stored, always derived from CreatedAt.
    [NotMapped]
    public DateTime ExpiresAt => CreatedAt.AddDays(LifespanDays);

    public bool IsActive { get; set; } = true;

    // Foreign key to the organizer (ApplicationUser)
    [Required]
    public string OrganizerId { get; set; } = string.Empty;
    public ApplicationUser? Organizer { get; set; }

    // Navigation property
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}