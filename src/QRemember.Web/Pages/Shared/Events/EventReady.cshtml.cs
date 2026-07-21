using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

public class EventReadyModel : PageModel
{
    public string EventName { get; private set; } = string.Empty;
    public string Hashtag { get; private set; } = string.Empty;
    public string EventLink { get; private set; } = string.Empty;
    public string QrCodeDataUri { get; private set; } = string.Empty;

    // Frontend-only placeholder: no Event row is created or persisted yet.
    public void OnGet(string? name)
    {
        EventName = string.IsNullOrWhiteSpace(name) ? "Your Event" : name.Trim();

        var slug = new string(EventName.Where(char.IsLetterOrDigit).ToArray());
        if (slug.Length == 0)
        {
            slug = "YourEvent";
        }

        Hashtag = "#" + slug;
        EventLink = $"myqreventchuchu.com/{slug.ToLowerInvariant()}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode($"https://{EventLink}", QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        var qrBytes = pngQr.GetGraphic(20);
        QrCodeDataUri = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
    }
}
