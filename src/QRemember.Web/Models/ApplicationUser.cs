using Microsoft.AspNetCore.Identity;

namespace QRemember.Web.Models;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string? DisplayName { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}