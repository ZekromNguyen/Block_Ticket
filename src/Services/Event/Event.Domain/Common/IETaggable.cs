using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Common;

/// <summary>
/// Interface for entities that support ETag-based optimistic concurrency
/// </summary>
public interface IETaggable
{
    /// <summary>
    /// Current ETag for the entity
    /// </summary>
    ETag CurrentETag { get; }

    /// <summary>
    /// Updates the entity's ETag (typically called after modifications)
    /// </summary>
    void UpdateETag();

    /// <summary>
    /// Validates that the provided ETag matches the current ETag
    /// </summary>
    /// <param name="etag">The ETag to validate</param>
    /// <exception cref="ETagMismatchException">Thrown when ETags don't match</exception>
    void ValidateETag(ETag? etag);

    /// <summary>
    /// Validates that the provided ETag string matches the current ETag
    /// </summary>
    /// <param name="etagValue">The ETag string to validate</param>
    /// <exception cref="ETagMismatchException">Thrown when ETags don't match</exception>
    void ValidateETag(string? etagValue);
}

/// <summary>
/// Base class for entities that support ETag-based optimistic concurrency
/// </summary>
public abstract class ETaggableEntity : BaseAuditableEntity, IETaggable
{
    private ETag? _currentETag;

    /// <summary>
    /// Current ETag for the entity
    /// </summary>
    public ETag CurrentETag
    {
        get
        {
            if (_currentETag == null)
            {
                GenerateInitialETag();
            }
            return _currentETag!;
        }
        protected set => _currentETag = value;
    }

    /// <summary>
    /// ETag value for database storage
    /// </summary>
    public string ETagValue 
    { 
        get => CurrentETag.Value;
        protected set 
        {
            if (!string.IsNullOrEmpty(value))
            {
                _currentETag = ETag.FromHash(GetEntityType(), GetEntityId(), value);
            }
        }
    }

    /// <summary>
    /// When the ETag was last updated
    /// </summary>
    public DateTime ETagUpdatedAt 
    { 
        get => CurrentETag.GeneratedAt;
        protected set { } // For EF Core mapping
    }

    /// <summary>
    /// Updates the entity's ETag (typically called after modifications)
    /// </summary>
    public virtual void UpdateETag()
    {
        var entityData = GetETagData();
        _currentETag = ETag.FromEntity(entityData, GetEntityId());
        if (this is BaseAuditableEntity auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Validates that the provided ETag matches the current ETag
    /// </summary>
    /// <param name="etag">The ETag to validate</param>
    /// <exception cref="ETagMismatchException">Thrown when ETags don't match</exception>
    public virtual void ValidateETag(ETag? etag)
    {
        if (etag == null)
        {
            throw new ETagRequiredException(GetEntityType(), GetEntityId());
        }

        if (!CurrentETag.Matches(etag))
        {
            throw new ETagMismatchException(
                CurrentETag.Value,
                etag.Value,
                GetEntityType(),
                GetEntityId()
            );
        }
    }

    /// <summary>
    /// Validates that the provided ETag string matches the current ETag
    /// </summary>
    /// <param name="etagValue">The ETag string to validate</param>
    /// <exception cref="ETagMismatchException">Thrown when ETags don't match</exception>
    public virtual void ValidateETag(string? etagValue)
    {
        if (string.IsNullOrEmpty(etagValue))
        {
            throw new ETagRequiredException(GetEntityType(), GetEntityId());
        }

        if (!CurrentETag.Matches(etagValue))
        {
            var providedETag = ETag.Parse(etagValue, GetEntityType(), GetEntityId());
            throw new ETagMismatchException(
                CurrentETag.Value,
                providedETag.Value,
                GetEntityType(),
                GetEntityId()
            );
        }
    }

    /// <summary>
    /// Checks if an ETag matches without throwing an exception
    /// </summary>
    public virtual bool IsETagValid(string? etagValue)
    {
        if (string.IsNullOrEmpty(etagValue))
        {
            return false;
        }

        return CurrentETag.Matches(etagValue);
    }

    /// <summary>
    /// Gets the entity type name for ETag generation
    /// </summary>
    protected virtual string GetEntityType()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Gets the entity ID for ETag generation
    /// </summary>
    protected virtual string GetEntityId()
    {
        return Id.ToString();
    }

    /// <summary>
    /// Gets the data to include in ETag calculation
    /// Override this to customize which properties affect the ETag
    /// </summary>
    protected virtual object GetETagData()
    {
        var baseData = new
        {
            Id,
            UpdatedAt = (this as BaseAuditableEntity)?.UpdatedAt ?? DateTime.UtcNow,
            // Add other relevant properties that should affect the ETag
            AdditionalData = GetAdditionalETagData()
        };
        return baseData;
    }

    /// <summary>
    /// Override this to include entity-specific data in ETag calculation
    /// </summary>
    protected virtual object? GetAdditionalETagData()
    {
        return null;
    }

    /// <summary>
    /// Generates the initial ETag for a new entity
    /// </summary>
    protected virtual void GenerateInitialETag()
    {
        var entityData = GetETagData();
        _currentETag = ETag.FromEntity(entityData, GetEntityId());
    }

    /// <summary>
    /// Called before saving changes to update the ETag
    /// </summary>
    public virtual void OnSaving()
    {
        UpdateETag();
    }
}
