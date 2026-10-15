using Claims.Application.Dto;
using Claims.Application.Interfaces;
using System.Threading.Channels;

namespace Claims.Infrastructure.Auditing
{
    public class AuditQueue : IAuditQueue
    {
        private readonly Channel<AuditEvent> _channel = 
            Channel.CreateUnbounded<AuditEvent>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                }
            );

        public void Enqueue(string entityType, string entityId, string action)
        {
            var auditEvent = new AuditEvent(
                entityType,
                entityId,
                action,
                DateTime.UtcNow
            );

            if (!_channel.Writer.TryWrite(auditEvent))
                throw new InvalidOperationException("The audit queue is no longer accepting events.");
        }

        public IAsyncEnumerable<AuditEvent> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
