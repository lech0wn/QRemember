using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;

namespace QRemember.Tests.TestHelpers;

public static class InMemoryDbContextFactory
{
    // Each call gets its own isolated database so tests never leak state into one another.
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
