using System.ComponentModel.DataAnnotations;

namespace QRemember.Web.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string EventCode { get; set; } = string.Empty;

    public string? QrCodeUrl { get; set; }

    public DateTime EventDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Foreign key to the organizer (ApplicationUser)
    [Required]
    public string OrganizerId { get; set; } = string.Empty;
    public ApplicationUser? Organizer { get; set; }

    // Navigation property
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}