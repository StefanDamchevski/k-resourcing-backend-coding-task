namespace Claims.Application.Dto
{
    public sealed record AuditEvent(
        string EntityType,
        string EntityId,
        string Action,
        DateTime CreatedAt
    );
}
