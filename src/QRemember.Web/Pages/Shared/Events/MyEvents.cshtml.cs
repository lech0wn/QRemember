using Microsoft.AspNetCore.Mvc.RazorPages;

public class MyEventsModel : PageModel
{
    // Frontend-only sample data until events are actually persisted and queried per organizer.
    public record SampleEvent(string Title, int Year, string GradientFrom, string GradientTo, int PhotoHeight);

    public List<SampleEvent> Events { get; } = new()
    {
        new("The Birthday Party of Someone", 2025, "#4A4139", "#1C1C1C", 260),
        new("Laag-laag lang", 2023, "#2E3238", "#111417", 230),
        new("Sweet 16th Birthday", 2023, "#41362A", "#1C1C1C", 220),
        new("Bachelorette Party at the Beach", 2025, "#1F4A4D", "#12262A", 240),
    };

    public void OnGet()
    {
    }
}
