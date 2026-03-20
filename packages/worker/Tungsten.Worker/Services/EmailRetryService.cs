namespace Tungsten.Worker.Services;

using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

public sealed class EmailRetryService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailRetryService> logger) : BackgroundService
{
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailRetryService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var unsent = await db.Notifications
                    .Include(n => n.User)
                    .Where(n => !n.EmailSent && n.EmailRetryCount < MaxRetries)
                    .OrderBy(n => n.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var notification in unsent)
                {
                    try
                    {
                        var email = notification.User?.Email;
                        if (string.IsNullOrEmpty(email)) continue;

                        await emailService.SendAsync(
                            email,
                            notification.Title,
                            $"<p>{notification.Message}</p>",
                            notification.Message,
                            stoppingToken);

                        notification.EmailSent = true;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Email retry failed for notification {Id}", notification.Id);
                        notification.EmailRetryCount++;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EmailRetryService error");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}
