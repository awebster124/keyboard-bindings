namespace KeyboardBindings.Api.Http;

public static class SecurityHeadersExtensions
{
    /// <summary>Adds defensive response headers to every response.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            await next();
        });
}
