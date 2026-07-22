using QRCoder;

namespace QRemember.Web.Services;

public interface IQrCodeService
{
    byte[] GeneratePngBytes(string content, int pixelsPerModule = 20);

    string GeneratePngDataUri(string content, int pixelsPerModule = 20);
}

public class QrCodeService : IQrCodeService
{
    public byte[] GeneratePngBytes(string content, int pixelsPerModule = 20)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(pixelsPerModule);
    }

    public string GeneratePngDataUri(string content, int pixelsPerModule = 20)
    {
        var bytes = GeneratePngBytes(content, pixelsPerModule);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
