using Claims.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Auditing
{
    public sealed class AuditBackgroundService(IAuditQueue auditQueue, IServiceScopeFactory scopeFactory, ILogger<AuditBackgroundService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var auditEvent in auditQueue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await using var scope =
                        scopeFactory.CreateAsyncScope();

                    var auditRepository =
                        scope.ServiceProvider
                            .GetRequiredService<IAuditRepository>();

                    switch (auditEvent.EntityType)
                    {
                        case "Claim":
                            await auditRepository.AuditClaimAsync(
                                auditEvent.EntityId,
                                auditEvent.Action,
                                stoppingToken);
                            break;
                        case "Cover":
                            await auditRepository.AuditCoverAsync(
                                auditEvent.EntityId,
                                auditEvent.Action,
                                stoppingToken);
                            break;
                        default:
                            logger.LogWarning("Unhandled entity type {EntityType}", auditEvent.EntityType);
                            break;
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Failed to persist audit event {EntityType} {EntityId} {Action}",
                        auditEvent.EntityType,
                        auditEvent.EntityId,
                        auditEvent.Action);
                }
            }
        }
    }
}
