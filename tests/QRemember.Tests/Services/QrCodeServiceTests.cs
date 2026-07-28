using QRemember.Web.Services;

namespace QRemember.Tests.Services;

public class QrCodeServiceTests
{
    private readonly QrCodeService _service = new();

    [Fact]
    public void GeneratePngBytes_ReturnsValidPngHeader()
    {
        var bytes = _service.GeneratePngBytes("https://example.com/event/abc123");

        // PNG files always start with this 8-byte signature.
        byte[] expectedSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Assert.Equal(expectedSignature, bytes.Take(8));
    }

    [Fact]
    public void GeneratePngBytes_DifferentContent_ProducesDifferentBytes()
    {
        var bytesA = _service.GeneratePngBytes("https://example.com/a");
        var bytesB = _service.GeneratePngBytes("https://example.com/b");

        Assert.NotEqual(bytesA, bytesB);
    }

    [Fact]
    public void GeneratePngBytes_LargerPixelsPerModule_ProducesLargerImage()
    {
        var small = _service.GeneratePngBytes("https://example.com/event/abc123", pixelsPerModule: 5);
        var large = _service.GeneratePngBytes("https://example.com/event/abc123", pixelsPerModule: 20);

        Assert.True(large.Length > small.Length);
    }

    [Fact]
    public void GeneratePngDataUri_HasExpectedPrefixAndValidBase64()
    {
        var uri = _service.GeneratePngDataUri("https://example.com/event/abc123");

        Assert.StartsWith("data:image/png;base64,", uri);

        var base64Part = uri["data:image/png;base64,".Length..];
        var decoded = Convert.FromBase64String(base64Part); // throws if invalid
        Assert.NotEmpty(decoded);
    }
}
