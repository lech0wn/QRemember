using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace QRemember.Tests.TestHelpers;

public static class PageModelTestHelpers
{
    // Wires up the PageContext/HttpContext/Url plumbing PageModel relies on at
    // runtime, so handler methods can be invoked directly without a real request pipeline.
    public static Mock<IUrlHelper> Bind(PageModel model, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);

        model.PageContext = new PageContext(actionContext) { ViewData = viewData };

        // IUrlHelper.Page(...) is an extension method that reads ambient route
        // values off ActionContext before calling RouteUrl, so this must be wired too.
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.ActionContext).Returns(actionContext);
        model.Url = urlHelper.Object;

        return urlHelper;
    }

    public static ClaimsPrincipal UserWithId(string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
