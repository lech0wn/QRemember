using QRemember.Web.Models;

namespace QRemember.Web.Services;

public interface IEventLookupService
{
    Task<Event?> GetActiveEventByCodeAsync(string? code, CancellationToken ct);
}