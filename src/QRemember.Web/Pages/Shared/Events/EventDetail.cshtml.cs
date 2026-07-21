using Microsoft.AspNetCore.Mvc.RazorPages;

public class EventDetailModel : PageModel
{
    // Frontend-only sample data until photo uploads/moderation are actually persisted.
    public record SamplePhoto(int Id, string GradientFrom, string GradientTo, int Height, string Status);

    public string EventName { get; private set; } = string.Empty;
    public string Hashtag { get; private set; } = string.Empty;

    public List<SamplePhoto> Photos { get; } = new()
    {
        new(1, "#2F2A22", "#1C1C1C", 260, "approved"),
        new(2, "#3C4A3C", "#1C1C1C", 200, "pending"),
        new(3, "#333644", "#1C1C1C", 230, "pending"),
        new(4, "#4A3C2F", "#1C1C1C", 210, "hidden"),
        new(5, "#2A3A3F", "#1C1C1C", 300, "approved"),
        new(6, "#4A2F35", "#1C1C1C", 190, "pending"),
        new(7, "#33403A", "#1C1C1C", 220, "approved"),
        new(8, "#3A3A2F", "#1C1C1C", 170, "pending"),
    };

    public void OnGet(string? name)
    {
        EventName = string.IsNullOrWhiteSpace(name) ? "Your Event" : name.Trim();

        var slug = new string(EventName.Where(char.IsLetterOrDigit).ToArray());
        Hashtag = "#" + (slug.Length > 0 ? slug : "YourEvent");
    }
}
