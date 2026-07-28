using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting; // Added for IWebHostEnvironment
using QRemember.Web.Data;
using QRemember.Web.Models;

namespace QRemember.Web.Pages.Shared.Events;

public class CreateEventModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IServer _server;
    private readonly IWebHostEnvironment _env; // Added environment check

    public CreateEventModel(
        AppDbContext db, 
        UserManager<ApplicationUser> userManager, 
        IServer server,
        IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _server = server;
        _env = env;
    }

    [BindProperty]
    [Required(ErrorMessage = "Event name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Event date is required")]
    public DateTime? EventDate { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var organizerId = _userManager.GetUserId(User);
        if (organizerId is null)
        {
            return Challenge();
        }

        var eventCode = await GenerateUniqueEventCodeAsync(Name);
        var guestOrigin = ResolveGuestOrigin();

        // Guests land on the upload page first (share photos + see recent submissions);
        // the full gallery is reachable from there via "View Gallery".
        var guestLink = Url.Page("/Guest/GuestUpload", pageHandler: null,
            values: new { code = eventCode },
            protocol: guestOrigin.Scheme,
            host: guestOrigin.Authority)
            ?? $"{guestOrigin.Scheme}://{guestOrigin.Authority}/Guest/GuestUpload?code={eventCode}";

        var newEvent = new Event
        {
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            EventDate = DateTime.SpecifyKind(EventDate!.Value, DateTimeKind.Utc),
            EventCode = eventCode,
            QrCodeUrl = guestLink,
            OrganizerId = organizerId,
            IsActive = true
        };

        _db.Events.Add(newEvent);
        await _db.SaveChangesAsync();

        return RedirectToPage("EventReady", new { code = eventCode });
    }

    private async Task<string> GenerateUniqueEventCodeAsync(string name)
    {
        var slugSource = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var slug = slugSource.Length > 0 ? slugSource[..Math.Min(slugSource.Length, 12)] : "event";

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var candidate = $"{slug}-{suffix}";

            var exists = await _db.Events.AnyAsync(e => e.EventCode == candidate);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique event code.");
    }

    private Uri ResolveGuestOrigin()
    {
        var fallback = new Uri($"{Request.Scheme}://{Request.Host}");

        // Only attempt LAN IP resolution locally during development.
        // In staging/production, trust the incoming Request host (or Forwarded Headers).
        if (!_env.IsDevelopment() || !IsLoopbackHost(Request.Host.Host))
        {
            return fallback;
        }

        var lanIp = GetLocalNetworkIp();

        // Always use http here, even if the organizer loaded this page over https:
        // the dev HTTPS cert only covers localhost, not the LAN IP, and isn't
        // trusted on guest devices, so https would fail to load for everyone.
        var boundPort = GetBoundPort("http");

        if (lanIp is null || boundPort is null)
        {
            return fallback;
        }

        return new Uri($"http://{lanIp}:{boundPort}");
    }

    private static bool IsLoopbackHost(string hostName)
    {
        return hostName.Equals("localhost", StringComparison.OrdinalIgnoreCase) 
            || (IPAddress.TryParse(hostName, out var ip) && IPAddress.IsLoopback(ip));
    }

    private int? GetBoundPort(string scheme)
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null)
        {
            return null;
        }

        foreach (var address in addresses)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase))
            {
                return uri.Port;
            }
        }
        
        // Fallback to whatever port the browser used to make the request if Kestrel configuration couldn't be parsed
        return Request.Host.Port;
    }

    private static string? GetLocalNetworkIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Attempt to route to public internet first
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch (SocketException)
        {
            try
            {
                // Offline Fallback: If no internet route exists, fall back to matching the primary gateway route 
                // by using a common private network subnet broadcast endpoint.
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("192.168.1.254", 65530); 
                return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
