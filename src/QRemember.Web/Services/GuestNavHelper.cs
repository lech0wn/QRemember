using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace QRemember.Web.Services;

public static class GuestNavHelper
{
    public static string GetGuestGalleryUrl(IUrlHelper urlHelper, string eventCode)
    {
        return urlHelper.Page("/Guest/GuestEventGallery", new { code = eventCode });
    }

    public static string GetGuestLandingUrl(IUrlHelper urlHelper, string eventCode)
    {
        return urlHelper.Page("/Guest/Index", new { code = eventCode });
    }

    public static string GetGuestUploadUrl(IUrlHelper urlHelper)
    {
        return urlHelper.Page("/GuestUpload");
    }
}