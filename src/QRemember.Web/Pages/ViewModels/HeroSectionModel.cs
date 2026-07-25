namespace QRemember.Web.Models.ViewModels;

public class HeroSectionModel
{
    public string EventName { get; set; } = string.Empty;
    public string EventDateDisplay { get; set; } = string.Empty;
    public string OrganizerDisplayName { get; set; } = string.Empty;
    public string? EventDescription { get; set; }
    public string? EventCode { get; set; }
    public string? HeroImageUrl { get; set; }
    public bool HasHeroImage => !string.IsNullOrWhiteSpace(HeroImageUrl);
}