using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// The "Automation" half of Stage 12: periodically scans system state and generates
/// notifications without any manual trigger. A manual "Generate Now" button on
/// NotificationController exists alongside this for on-demand/testing use, but this
/// background loop is what makes it actually automated rather than just on-demand.
/// </summary>
public class NotificationGenerationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationGenerationBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var created = await notificationService.GenerateSystemNotificationsAsync(stoppingToken);

                if (created > 0)
                {
                    logger.LogInformation("Generated {Count} system notification(s).", created);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error generating system notifications.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Shutting down.
            }
        }
    }
}
