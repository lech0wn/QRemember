using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using QRemember.Web.Services;

namespace QRemember.Tests.Services;

public class EventLookupServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetActiveEventByCodeAsync_ReturnsNull_ForNullOrWhitespaceCode(string? code)
    {
        using var db = InMemoryDbContextFactory.Create();
        var service = new EventLookupService(db);

        var result = await service.GetActiveEventByCodeAsync(code, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveEventByCodeAsync_ReturnsNull_WhenEventIsInactive()
    {
        using var db = InMemoryDbContextFactory.Create();
        db.Events.Add(new Event { EventCode = "ABC123", IsActive = false, OrganizerId = "org1" });
        await db.SaveChangesAsync();

        var service = new EventLookupService(db);
        var result = await service.GetActiveEventByCodeAsync("ABC123", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveEventByCodeAsync_ReturnsNull_WhenNoEventMatchesCode()
    {
        using var db = InMemoryDbContextFactory.Create();
        db.Events.Add(new Event { EventCode = "ABC123", IsActive = true, OrganizerId = "org1" });
        await db.SaveChangesAsync();

        var service = new EventLookupService(db);
        var result = await service.GetActiveEventByCodeAsync("DOESNOTEXIST", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveEventByCodeAsync_IsCaseInsensitive_AndTrimsWhitespace()
    {
        using var db = InMemoryDbContextFactory.Create();
        db.Events.Add(new Event { EventCode = "ABC123", IsActive = true, OrganizerId = "org1", Name = "Test" });
        await db.SaveChangesAsync();

        var service = new EventLookupService(db);
        var result = await service.GetActiveEventByCodeAsync("  abc123  ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Test", result!.Name);
    }
}
