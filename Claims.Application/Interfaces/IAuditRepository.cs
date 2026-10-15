namespace Claims.Application.Interfaces
{
    public interface IAuditRepository
    {
        Task AuditClaimAsync(string claimId, string action, CancellationToken cancellationToken);

        Task AuditCoverAsync(string coverId, string action, CancellationToken cancellationToken);
    }
}
