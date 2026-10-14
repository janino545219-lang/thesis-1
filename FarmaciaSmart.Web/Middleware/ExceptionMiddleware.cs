using System.Net;
using System.Text.Json;

namespace FarmaciaSmart.Web.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new
                {
                    error = "An unexpected error occurred. Please try again later."
                });
                await context.Response.WriteAsync(payload);
                return;
            }

            context.Response.Redirect("/Home/Error");
        }
    }
}
