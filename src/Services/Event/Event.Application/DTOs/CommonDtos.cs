using System;
using System.Collections.Generic;
using Event.Domain.Models;

namespace Event.Application.DTOs
{
    // Cache warmup DTOs
    public class CacheWarmupResultDto
    {
        public bool Success { get; set; }
        public int CachedItems { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Approval workflow DTOs  
    public class ApprovalWorkflowStatistics
    {
        public int TotalWorkflows { get; set; }
        public int CompletedWorkflows { get; set; }
        public int PendingWorkflows { get; set; }
        public int CancelledWorkflows { get; set; }
        public Dictionary<ApprovalOperationType, int> OperationTypeCounts { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // ETag DTOs
    public class ETag
    {
        public string Value { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }

        public ETag()
        {
        }

        public ETag(string value)
        {
            Value = value;
            UpdatedAt = DateTime.UtcNow;
        }

        public static ETag Generate()
        {
            return new ETag(Guid.NewGuid().ToString("N"));
        }

        public static ETag FromHash(string entityType, string entityId, string hash)
        {
            return new ETag($"{entityType}:{entityId}:{hash}");
        }
    }

    // Base entity interface for ETag support
    public interface IETagEntity
    {
        string ETagValue { get; set; }
        DateTime ETagUpdatedAt { get; set; }
        ETag ETag { get; set; }
    }
}
