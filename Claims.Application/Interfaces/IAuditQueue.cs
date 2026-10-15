using Claims.Application.Dto;

namespace Claims.Application.Interfaces
{
    public interface IAuditQueue
    {
        void Enqueue(string entityType, string entityId, string action);

        IAsyncEnumerable<AuditEvent> ReadAllAsync(CancellationToken cancellationToken);
    }
}
