namespace System_Monitor_API_v2.Authentication;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // Allow CORS preflight and Swagger/OpenAPI without API key
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (context.Request.Method == "OPTIONS" ||
            (context.Request.Method == "GET" && (path.Contains("openapi") || path.Contains("swagger"))))
        {
            await next(context);
            return;
        }

        // Support header or query string (query string needed for browser EventSource)
        var extractedApiKey = context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerKey)
            ? headerKey.ToString()
            : context.Request.Query["apiKey"].ToString();

        if (string.IsNullOrWhiteSpace(extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key is missing. Pass via X-API-Key header or ?apiKey= query parameter.");
            return;
        }

        var configuredApiKey = configuration.GetValue<string>("ApiKey");

        if (string.IsNullOrWhiteSpace(configuredApiKey) || !configuredApiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        await next(context);
    }
}
