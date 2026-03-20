using Microsoft.Extensions.Caching.Memory;

namespace DemoCICD.API.Middleware;

/// <summary>
/// Giới hạn số lần gọi POST /api/v1/auth/login từ cùng IP (10 lần / phút).
/// </summary>
public sealed class LoginRateLimitMiddleware
{
    private const int MaxAttemptsPerMinute = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly RequestDelegate _next;

    public LoginRateLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
    {
        if (!IsLoginPath(context))
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"login_ratelimit:{ip}";

        var count = cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            return 0;
        });

        if (count >= MaxAttemptsPerMinute)
        {
            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"type\":\"Auth.RateLimitExceeded\",\"detail\":\"Too many login attempts. Try again later.\"}");
            return;
        }

        cache.Set(key, count + 1, Window);
        await _next(context);
    }

    private static bool IsLoginPath(HttpContext context)
    {
        if (context.Request.Method != "POST") return false;
        var path = context.Request.Path.Value ?? "";
        return path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase);
    }
}
