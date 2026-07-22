using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;
using QRemember.Web.Services;

namespace QRemember.Web.Services;

public class EventLookupService : IEventLookupService
{
    private readonly AppDbContext _db;
    public EventLookupService(AppDbContext db) => _db = db;

    public async Task<Event?> GetActiveEventByCodeAsync(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = code.Trim().ToUpperInvariant();
        return await _db.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventCode == normalized && e.IsActive, ct);
    }
}