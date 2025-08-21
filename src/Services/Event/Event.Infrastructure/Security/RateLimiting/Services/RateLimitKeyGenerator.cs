using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Event.Infrastructure.Security.RateLimiting.Services;

/// <summary>
/// Generates unique keys for rate limiting based on different strategies
/// </summary>
public class RateLimitKeyGenerator : IRateLimitKeyGenerator
{
    private readonly Dictionary<RateLimitType, Func<RateLimitRule, RateLimitContext, string>> _keyGenerators;

    public RateLimitKeyGenerator()
    {
        _keyGenerators = new Dictionary<RateLimitType, Func<RateLimitRule, RateLimitContext, string>>
        {
            { RateLimitType.IpAddress, GenerateIpKey },
            { RateLimitType.Client, GenerateClientKey },
            { RateLimitType.Organization, GenerateOrganizationKey },
            { RateLimitType.Endpoint, GenerateEndpointKey },
            { RateLimitType.Global, GenerateGlobalKey }
        };
    }

    /// <summary>
    /// Generates a unique key for rate limiting based on the rule and context
    /// </summary>
    public string GenerateKey(RateLimitRule rule, RateLimitContext context)
    {
        if (!_keyGenerators.TryGetValue(rule.Type, out var generator))
        {
            throw new ArgumentException($"Unsupported rate limit type: {rule.Type}");
        }

        var key = generator(rule, context);
        
        // Add rule ID to ensure different rules don't interfere
        return $"{rule.Id}:{key}";
    }

    /// <summary>
    /// Generates multiple keys for different rate limiting strategies
    /// </summary>
    public Dictionary<string, string> GenerateKeys(IEnumerable<RateLimitRule> rules, RateLimitContext context)
    {
        var keys = new Dictionary<string, string>();

        foreach (var rule in rules)
        {
            try
            {
                var key = GenerateKey(rule, context);
                keys[rule.Id] = key;
            }
            catch (Exception ex)
            {
                // Log error but continue with other rules
                // In production, you'd use proper logging
                Console.WriteLine($"Error generating key for rule {rule.Id}: {ex.Message}");
            }
        }

        return keys;
    }

    /// <summary>
    /// Generates IP-based rate limiting key
    /// </summary>
    private string GenerateIpKey(RateLimitRule rule, RateLimitContext context)
    {
        if (string.IsNullOrEmpty(context.IpAddress))
        {
            throw new ArgumentException("IP address is required for IP-based rate limiting");
        }

        // Normalize IP address (handle IPv6, remove port, etc.)
        var normalizedIp = NormalizeIpAddress(context.IpAddress);
        
        return $"ip:{normalizedIp}";
    }

    /// <summary>
    /// Generates client-based rate limiting key
    /// </summary>
    private string GenerateClientKey(RateLimitRule rule, RateLimitContext context)
    {
        if (string.IsNullOrEmpty(context.ClientId))
        {
            // Fall back to IP if no client ID available
            return GenerateIpKey(rule, context);
        }

        return $"client:{context.ClientId}";
    }

    /// <summary>
    /// Generates organization-based rate limiting key
    /// </summary>
    private string GenerateOrganizationKey(RateLimitRule rule, RateLimitContext context)
    {
        if (string.IsNullOrEmpty(context.OrganizationId))
        {
            throw new ArgumentException("Organization ID is required for organization-based rate limiting");
        }

        return $"org:{context.OrganizationId}";
    }

    /// <summary>
    /// Generates endpoint-specific rate limiting key
    /// </summary>
    private string GenerateEndpointKey(RateLimitRule rule, RateLimitContext context)
    {
        var endpoint = NormalizeEndpoint(context.Endpoint ?? "unknown");
        var method = context.HttpMethod ?? "GET";
        
        // Include client/IP for endpoint-specific limits
        var clientPart = !string.IsNullOrEmpty(context.ClientId) 
            ? $"client:{context.ClientId}" 
            : $"ip:{NormalizeIpAddress(context.IpAddress ?? "unknown")}";

        return $"endpoint:{method}:{endpoint}:{clientPart}";
    }

    /// <summary>
    /// Generates global rate limiting key
    /// </summary>
    private string GenerateGlobalKey(RateLimitRule rule, RateLimitContext context)
    {
        return "global:all";
    }

    /// <summary>
    /// Normalizes IP address for consistent key generation
    /// </summary>
    private string NormalizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return "unknown";

        // Remove port if present
        var colonIndex = ipAddress.LastIndexOf(':');
        if (colonIndex > 0 && ipAddress.Count(c => c == ':') == 1)
        {
            // IPv4 with port
            ipAddress = ipAddress.Substring(0, colonIndex);
        }

        // Handle IPv6 addresses
        if (ipAddress.StartsWith('[') && ipAddress.Contains("]:"))
        {
            var bracketIndex = ipAddress.IndexOf("]:");
            ipAddress = ipAddress.Substring(1, bracketIndex - 1);
        }

        // For IPv6, we might want to normalize to a shorter form
        if (ipAddress.Contains(':') && ipAddress.Count(c => c == ':') > 1)
        {
            // This is a simplified IPv6 normalization
            // In production, you'd use System.Net.IPAddress.Parse for proper normalization
            return ipAddress.ToLowerInvariant();
        }

        return ipAddress;
    }

    /// <summary>
    /// Normalizes endpoint path for consistent key generation
    /// </summary>
    private string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return "unknown";

        // Remove query parameters
        var queryIndex = endpoint.IndexOf('?');
        if (queryIndex >= 0)
        {
            endpoint = endpoint.Substring(0, queryIndex);
        }

        // Normalize path separators
        endpoint = endpoint.Replace('\\', '/');

        // Remove trailing slash
        if (endpoint.EndsWith('/') && endpoint.Length > 1)
        {
            endpoint = endpoint.TrimEnd('/');
        }

        // Replace path parameters with placeholders (e.g., /events/123 -> /events/{id})
        endpoint = Regex.Replace(endpoint, @"/\d+", "/{id}");
        endpoint = Regex.Replace(endpoint, @"/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "/{guid}");

        return endpoint.ToLowerInvariant();
    }

    /// <summary>
    /// Generates a hash-based key for very long identifiers
    /// </summary>
    private string GenerateHashKey(string input)
    {
        if (input.Length <= 100)
            return input;

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>
    /// Validates that a key is safe for use in Redis
    /// </summary>
    private string ValidateKey(string key)
    {
        // Redis keys should be safe and not too long
        if (key.Length > 250)
        {
            key = GenerateHashKey(key);
        }

        // Remove any potentially problematic characters
        key = Regex.Replace(key, @"[^\w\-:.]", "_");

        return key;
    }
}
