namespace FinanceApp.Domain.Common;

public abstract class BaseEntity
{
    // Primary key
    public Guid Id { get; protected set; } = Guid.NewGuid();
    

    // Automatically set by DbContext on insert
    public DateTimeOffset CreatedAt { get; private set; }

    // Automatically set by DbContext on update
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Soft delete flag
    public bool IsDeleted { get; private set; } = false;

    // Called by DbContext internally
    public void SetCreated(DateTimeOffset createdAt)
    {
        CreatedAt = createdAt;
    }

    public void MarkAsUpdated(DateTimeOffset updatedAt)
    {
        UpdatedAt = updatedAt;
    }

    // Public method to soft delete
    public void SoftDelete()
    {
        IsDeleted = true;
    }
}