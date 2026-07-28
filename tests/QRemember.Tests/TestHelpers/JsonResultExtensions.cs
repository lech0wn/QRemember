using Microsoft.AspNetCore.Mvc;

namespace QRemember.Tests.TestHelpers;

public static class JsonResultExtensions
{
    // Handlers in this codebase return `new JsonResult(new { success = true, ... })`.
    // Anonymous types are assembly-internal, so reflection (not a direct cast) is
    // required to read their properties from the test assembly.
    public static T GetProperty<T>(this JsonResult result, string propertyName)
    {
        var value = result.Value ?? throw new InvalidOperationException("JsonResult.Value is null");
        var property = value.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {value.GetType()}");
        return (T)property.GetValue(value)!;
    }
}
