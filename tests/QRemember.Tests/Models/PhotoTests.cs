using QRemember.Web.Models;

namespace QRemember.Tests.Models;

public class PhotoTests
{
    [Fact]
    public void Status_IsHidden_WhenIsHiddenTrue_RegardlessOfApproval()
    {
        var photo = new Photo { IsHidden = true, IsApproved = true };
        Assert.Equal("hidden", photo.Status);
    }

    [Fact]
    public void Status_IsApproved_WhenApprovedAndNotHidden()
    {
        var photo = new Photo { IsHidden = false, IsApproved = true };
        Assert.Equal("approved", photo.Status);
    }

    [Fact]
    public void Status_IsPending_WhenNotApprovedAndNotHidden()
    {
        var photo = new Photo { IsHidden = false, IsApproved = false };
        Assert.Equal("pending", photo.Status);
    }

    [Fact]
    public void Status_HiddenTakesPrecedence_OverUnapproved()
    {
        var photo = new Photo { IsHidden = true, IsApproved = false };
        Assert.Equal("hidden", photo.Status);
    }
}
