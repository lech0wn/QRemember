using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;

public class CreateEventModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IServer _server;

    public CreateEventModel(AppDbContext db, UserManager<ApplicationUser> userManager, IServer server)
    {
        _db = db;
        _userManager = userManager;
        _server = server;
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

    public void OnGet()
    {
    }

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
        var guestLink = Url.Page("/Guest/Index", pageHandler: null, values: new { code = eventCode }, protocol: guestOrigin.Scheme, host: guestOrigin.Authority)
            ?? $"{guestOrigin.Scheme}://{guestOrigin.Authority}/e/{eventCode}";

        var newEvent = new Event
        {
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            EventDate = DateTime.SpecifyKind(EventDate!.Value, DateTimeKind.Utc),
            EventCode = eventCode,
            QrCodeUrl = guestLink,
            OrganizerId = organizerId,
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

    // Guests scan the QR code with a phone on the same Wi-Fi as the host machine, not the
    // machine itself, so "localhost" in the generated link needs to become a LAN-reachable
    // address they can actually open.
    private Uri ResolveGuestOrigin()
    {
        var fallback = new Uri($"{Request.Scheme}://{Request.Host}");

        if (!IsLoopbackHost(Request.Host.Host))
        {
            return fallback;
        }

        var lanIp = GetLocalNetworkIp();
        var httpPort = GetBoundHttpPort();
        if (lanIp is null || httpPort is null)
        {
            return fallback;
        }

        return new Uri($"http://{lanIp}:{httpPort}");
    }

    private static bool IsLoopbackHost(string hostName)
    {
        return hostName.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || (IPAddress.TryParse(hostName, out var ip) && IPAddress.IsLoopback(ip));
    }

    private int? GetBoundHttpPort()
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null)
        {
            return null;
        }

        foreach (var address in addresses)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Scheme == "http")
            {
                return uri.Port;
            }
        }

        return null;
    }

    // Machines with WSL, Hyper-V, or VMware installed have several virtual adapters
    // (vEthernet, VMnet, ...) that also report as "up" with a non-loopback IPv4 address,
    // so picking the first such adapter is unreliable. Asking the OS which local address
    // it would use to route to the public internet reliably resolves to the real
    // Wi-Fi/Ethernet adapter instead, since virtual adapters have no default route.
    private static string? GetLocalNetworkIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch (SocketException)
        {
            return null;
        }
    }
}
