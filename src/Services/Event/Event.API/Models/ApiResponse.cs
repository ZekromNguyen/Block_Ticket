using System.Text.Json.Serialization;

namespace Event.API.Models;

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Response data type</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public object? Errors { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Create successful response
    /// </summary>
    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static ApiResponse<T> Error(string message, object? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// Standard API response without data
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public object? Errors { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Create successful response
    /// </summary>
    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static ApiResponse Error(string message, object? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// Paginated API response
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public class PaginatedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Pagination metadata
    /// </summary>
    public PaginationMetadata Pagination { get; set; } = new();

    /// <summary>
    /// Create successful paginated response
    /// </summary>
    public static PaginatedApiResponse<T> Ok(
        IEnumerable<T> data, 
        int totalCount, 
        int pageNumber, 
        int pageSize, 
        string? message = null)
    {
        return new PaginatedApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }
}

/// <summary>
/// API response extensions
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Convert PagedResult to PaginatedApiResponse
    /// </summary>
    public static PaginatedApiResponse<T> ToApiResponse<T>(this Event.Application.Common.Models.PagedResult<T> pagedResult, string? message = null)
    {
        return PaginatedApiResponse<T>.Ok(
            pagedResult.Items,
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            pagedResult.PageSize,
            message);
    }
}
