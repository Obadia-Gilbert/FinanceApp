namespace FinanceApp.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public void SetCreated(DateTimeOffset createdAt)
    {
        CreatedAt = createdAt;
    }

    public void MarkAsUpdated(DateTimeOffset updatedAt)
    {
        UpdatedAt = updatedAt;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
    }
}