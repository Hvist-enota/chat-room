using Microsoft.Extensions.Primitives;

namespace ChatRoom.Api.Services;

public sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    private const string UserHeaderName = "X-User-Id";

    public Guid GetRequiredUserId()
    {
        var httpContext = accessor.HttpContext;
        if (httpContext is null)
        {
            throw new UnauthorizedAccessException("Http context is not available.");
        }

        if (!httpContext.Request.Headers.TryGetValue(UserHeaderName, out StringValues userHeader)
            || StringValues.IsNullOrEmpty(userHeader)
            || !Guid.TryParse(userHeader.ToString(), out var userId))
        {
            throw new UnauthorizedAccessException("Provide valid X-User-Id header.");
        }

        return userId;
    }
}
