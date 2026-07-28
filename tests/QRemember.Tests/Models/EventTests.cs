using QRemember.Web.Models;

namespace QRemember.Tests.Models;

public class EventTests
{
    [Fact]
    public void ExpiresAt_IsCreatedAtPlusLifespanDays()
    {
        var createdAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var ev = new Event { CreatedAt = createdAt };

        Assert.Equal(createdAt.AddDays(Event.LifespanDays), ev.ExpiresAt);
    }

    [Fact]
    public void ExpiresAt_UpdatesWhenCreatedAtChanges()
    {
        var ev = new Event { CreatedAt = new DateTime(2026, 1, 1) };
        var firstExpiry = ev.ExpiresAt;

        ev.CreatedAt = ev.CreatedAt.AddDays(1);

        Assert.NotEqual(firstExpiry, ev.ExpiresAt);
        Assert.Equal(firstExpiry.AddDays(1), ev.ExpiresAt);
    }
}
