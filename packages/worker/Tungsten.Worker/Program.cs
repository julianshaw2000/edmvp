using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<AppDbContext>());

// Email service (same conditional logic as API)
if (!string.IsNullOrEmpty(builder.Configuration["SendGrid:ApiKey"]))
    builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
else
    builder.Services.AddSingleton<IEmailService, LogEmailService>();

// Background services
builder.Services.AddHostedService<JobProcessorService>();
builder.Services.AddHostedService<EmailRetryService>();
builder.Services.AddHostedService<EscalationService>();

var host = builder.Build();
host.Run();
