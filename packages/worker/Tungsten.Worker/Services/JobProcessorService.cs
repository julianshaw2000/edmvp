namespace Tungsten.Worker.Services;

using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

public sealed class JobProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<JobProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("JobProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pendingJobs = await db.Jobs
                    .Where(j => j.Status == "PENDING")
                    .OrderBy(j => j.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var job in pendingJobs)
                {
                    try
                    {
                        job.Status = "PROCESSING";
                        await db.SaveChangesAsync(stoppingToken);

                        logger.LogInformation("Processing job {JobId} type {JobType}", job.Id, job.JobType);

                        // Job type dispatch — extend as needed
                        job.Status = "COMPLETED";
                        job.CompletedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Job {JobId} failed", job.Id);
                        job.Status = "FAILED";
                        job.ErrorDetail = ex.Message;
                        job.CompletedAt = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JobProcessorService error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
