using Event.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Event.Infrastructure.Middleware;

/// <summary>
/// Middleware to set PostgreSQL session variables for Row-Level Security enforcement
/// </summary>
public class OrganizationContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OrganizationContextMiddleware> _logger;

    public OrganizationContextMiddleware(
        RequestDelegate next,
        ILogger<OrganizationContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOrganizationContextProvider organizationContextProvider)
    {
        try
        {
            // Set organization context in PostgreSQL session for RLS
            await SetPostgreSqlSessionContext(context, organizationContextProvider);
            
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrganizationContextMiddleware");
            throw;
        }
        finally
        {
            // Clear context after request (optional, as connection pooling handles this)
            await ClearPostgreSqlSessionContext(context);
        }
    }

    private async Task SetPostgreSqlSessionContext(HttpContext context, IOrganizationContextProvider organizationContextProvider)
    {
        try
        {
            var organizationId = organizationContextProvider.GetCurrentOrganizationIdOrNull();
            var userId = organizationContextProvider.GetCurrentUserIdOrNull();
            var correlationId = organizationContextProvider.GetCorrelationId();

            if (organizationId.HasValue)
            {
                // Store context in HttpContext for use by DbContext
                context.Items["CurrentOrganizationId"] = organizationId.Value;
                context.Items["CurrentUserId"] = userId;
                context.Items["CorrelationId"] = correlationId;

                _logger.LogDebug("Set organization context: OrgId={OrganizationId}, UserId={UserId}, CorrelationId={CorrelationId}",
                    organizationId.Value, userId, correlationId);
            }
            else
            {
                _logger.LogDebug("No organization context available for request {Path}", context.Request.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set PostgreSQL session context");
            // Don't throw - allow request to continue without RLS context
        }
    }

    private async Task ClearPostgreSqlSessionContext(HttpContext context)
    {
        try
        {
            // Clear context from HttpContext
            context.Items.Remove("CurrentOrganizationId");
            context.Items.Remove("CurrentUserId");
            context.Items.Remove("CorrelationId");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear PostgreSQL session context");
        }
    }
}

/// <summary>
/// Service to manage PostgreSQL session variables for RLS
/// </summary>
public interface IPostgreSqlSessionManager
{
    Task SetSessionVariablesAsync(IDbConnection connection, Guid? organizationId, Guid? userId, string? correlationId);
    Task ClearSessionVariablesAsync(IDbConnection connection);
}

public class PostgreSqlSessionManager : IPostgreSqlSessionManager
{
    private readonly ILogger<PostgreSqlSessionManager> _logger;

    public PostgreSqlSessionManager(ILogger<PostgreSqlSessionManager> logger)
    {
        _logger = logger;
    }

    public async Task SetSessionVariablesAsync(IDbConnection connection, Guid? organizationId, Guid? userId, string? correlationId)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            _logger.LogWarning("Connection is not a PostgreSQL connection, skipping session variable setup");
            return;
        }

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await npgsqlConnection.OpenAsync();
            }

            // Set organization context for RLS
            if (organizationId.HasValue)
            {
                using var orgCommand = new NpgsqlCommand(
                    "SELECT set_config('app.current_organization_id', @orgId, true)", 
                    npgsqlConnection);
                orgCommand.Parameters.AddWithValue("@orgId", organizationId.Value.ToString());
                await orgCommand.ExecuteNonQueryAsync();
            }

            // Set user context
            if (userId.HasValue)
            {
                using var userCommand = new NpgsqlCommand(
                    "SELECT set_config('app.current_user_id', @userId, true)", 
                    npgsqlConnection);
                userCommand.Parameters.AddWithValue("@userId", userId.Value.ToString());
                await userCommand.ExecuteNonQueryAsync();
            }

            // Set correlation ID for tracing
            if (!string.IsNullOrEmpty(correlationId))
            {
                using var correlationCommand = new NpgsqlCommand(
                    "SELECT set_config('app.correlation_id', @correlationId, true)", 
                    npgsqlConnection);
                correlationCommand.Parameters.AddWithValue("@correlationId", correlationId);
                await correlationCommand.ExecuteNonQueryAsync();
            }

            _logger.LogDebug("Set PostgreSQL session variables: OrgId={OrganizationId}, UserId={UserId}",
                organizationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set PostgreSQL session variables");
            throw;
        }
    }

    public async Task ClearSessionVariablesAsync(IDbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            return;
        }

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await npgsqlConnection.OpenAsync();
            }

            // Clear session variables
            using var clearCommand = new NpgsqlCommand(@"
                SELECT set_config('app.current_organization_id', '', true);
                SELECT set_config('app.current_user_id', '', true);
                SELECT set_config('app.correlation_id', '', true);
            ", npgsqlConnection);
            
            await clearCommand.ExecuteNonQueryAsync();

            _logger.LogDebug("Cleared PostgreSQL session variables");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear PostgreSQL session variables");
        }
    }
}

/// <summary>
/// Extensions for middleware registration
/// </summary>
public static class OrganizationContextMiddlewareExtensions
{
    /// <summary>
    /// Adds the organization context middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseOrganizationContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OrganizationContextMiddleware>();
    }
}
