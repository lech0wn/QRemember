namespace QRemember.Web.Models.ViewModels;

public class ScanRequest
{
    public string QrData { get; set; } = string.Empty;
}

public class LookupRequest
{
    public string EventCode { get; set; } = string.Empty;
}

public class UploadRequest
{
    public string EventCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public List<string> PhotoData { get; set; } = new List<string>();
}