using Microsoft.EntityFrameworkCore;
using Resend;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<AppDbContext>());

// Email service (same conditional logic as API)
if (!string.IsNullOrEmpty(builder.Configuration["Resend:ApiKey"]))
{
    builder.Services.AddOptions();
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.Configure<ResendClientOptions>(o =>
    {
        o.ApiToken = builder.Configuration["Resend:ApiKey"]!;
    });
    builder.Services.AddTransient<IResend, ResendClient>();
    builder.Services.AddSingleton<IEmailService, ResendEmailService>();
}
else
    builder.Services.AddSingleton<IEmailService, LogEmailService>();

// Background services
builder.Services.AddHostedService<JobProcessorService>();
builder.Services.AddHostedService<EmailRetryService>();
builder.Services.AddHostedService<EscalationService>();

var host = builder.Build();
host.Run();
