using MedCenter.Data;
using MedCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30); // Match Program.cs session timeout

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAbandonedSessionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up abandoned sessions");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Session Cleanup Service stopped");
    }

    private async Task CleanupAbandonedSessionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Find all sessions without logout that are older than timeout
        var cutoffTime = DateTime.UtcNow.Subtract(_sessionTimeout);

        var abandonedSessions = await context.trazabilidadLogins
            .Where(tl => tl.MomentoLogout == null && tl.MomentoLogin < cutoffTime)
            .ToListAsync();

        if (abandonedSessions.Any())
        {
            _logger.LogInformation($"Found {abandonedSessions.Count} abandoned sessions to clean up");

            foreach (var session in abandonedSessions)
            {
                session.MomentoLogout = DateTime.UtcNow;
                session.TipoLogout = TipoLogout.Forzado;
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"Cleaned up {abandonedSessions.Count} abandoned sessions");
        }
    }
}
