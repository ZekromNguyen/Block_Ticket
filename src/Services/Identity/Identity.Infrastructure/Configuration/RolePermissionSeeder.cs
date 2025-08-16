using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Configuration;

public static class RolePermissionSeeder
{
    public static async Task SeedRolesAndPermissionsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();

        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed permissions first
            await SeedPermissionsAsync(context, logger);
            await context.SaveChangesAsync(); // Save permissions first

            // Seed roles
            await SeedRolesAsync(context, logger);
            await context.SaveChangesAsync(); // Save roles before assigning permissions

            // Assign permissions to roles
            await AssignPermissionsToRolesAsync(context, logger);
            await context.SaveChangesAsync(); // Save role-permission assignments
            logger.LogInformation("Successfully seeded roles and permissions");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding roles and permissions");
            throw;
        }
    }

    private static async Task SeedPermissionsAsync(IdentityDbContext context, ILogger logger)
    {
        var permissionData = new[]
        {
            // Identity Service Permissions
            ("identity:users:read", "Read user information", "users", "read", "identity"),
            ("identity:users:write", "Create and update users", "users", "write", "identity"),
            ("identity:users:delete", "Delete users", "users", "delete", "identity"),
            ("identity:roles:read", "Read roles", "roles", "read", "identity"),
            ("identity:roles:write", "Create and update roles", "roles", "write", "identity"),
            ("identity:sessions:read", "Read user sessions", "sessions", "read", "identity"),
            ("identity:sessions:revoke", "Revoke user sessions", "sessions", "revoke", "identity"),
            ("identity:audit:read", "Read audit logs", "audit", "read", "identity"),

            // Event Service Permissions
            ("events:events:read", "View events", "events", "read", "events"),
            ("events:events:write", "Create and update events", "events", "write", "events"),
            ("events:events:delete", "Delete events", "events", "delete", "events"),
            ("events:venues:read", "View venues", "venues", "read", "events"),
            ("events:venues:write", "Create and update venues", "venues", "write", "events"),
            ("events:categories:read", "View event categories", "categories", "read", "events"),
            ("events:categories:write", "Manage event categories", "categories", "write", "events"),

            // Ticketing Service Permissions
            ("ticketing:tickets:read", "View tickets", "tickets", "read", "ticketing"),
            ("ticketing:tickets:purchase", "Purchase tickets", "tickets", "purchase", "ticketing"),
            ("ticketing:tickets:transfer", "Transfer tickets", "tickets", "transfer", "ticketing"),
            ("ticketing:orders:read", "View orders", "orders", "read", "ticketing"),
            ("ticketing:orders:write", "Manage orders", "orders", "write", "ticketing"),
            ("ticketing:orders:cancel", "Cancel orders", "orders", "cancel", "ticketing"),
            ("ticketing:payments:read", "View payment information", "payments", "read", "ticketing"),
            ("ticketing:refunds:process", "Process refunds", "refunds", "process", "ticketing"),

            // Resale Service Permissions
            ("resale:listings:read", "View resale listings", "listings", "read", "resale"),
            ("resale:listings:create", "Create resale listings", "listings", "create", "resale"),
            ("resale:listings:manage", "Manage resale listings", "listings", "manage", "resale"),
            ("resale:waitlist:join", "Join waiting lists", "waitlist", "join", "resale"),
            ("resale:waitlist:manage", "Manage waiting lists", "waitlist", "manage", "resale"),
            ("resale:transactions:read", "View resale transactions", "transactions", "read", "resale"),

            // Verification Service Permissions
            ("verification:tickets:verify", "Verify tickets", "tickets", "verify", "verification"),
            ("verification:checkins:read", "View check-in records", "checkins", "read", "verification"),
            ("verification:checkins:write", "Record check-ins", "checkins", "write", "verification"),
            ("verification:events:access", "Access event verification", "events", "access", "verification"),

            // Notification Service Permissions
            ("notifications:notifications:read", "View notifications", "notifications", "read", "notifications"),
            ("notifications:notifications:send", "Send notifications", "notifications", "send", "notifications"),
            ("notifications:templates:read", "View notification templates", "templates", "read", "notifications"),
            ("notifications:templates:write", "Manage notification templates", "templates", "write", "notifications"),

            // Blockchain Orchestrator Permissions
            ("blockchain:tickets:mint", "Mint NFT tickets", "tickets", "mint", "blockchain"),
            ("blockchain:tickets:burn", "Burn NFT tickets", "tickets", "burn", "blockchain"),
            ("blockchain:tickets:transfer", "Transfer NFT tickets", "tickets", "transfer", "blockchain"),
            ("blockchain:contracts:read", "View smart contracts", "contracts", "read", "blockchain"),
            ("blockchain:transactions:read", "View blockchain transactions", "transactions", "read", "blockchain"),

            // Gateway Service Permissions
            ("gateway:routes:read", "View gateway routes", "routes", "read", "gateway"),
            ("gateway:routes:write", "Manage gateway routes", "routes", "write", "gateway"),
            ("gateway:monitoring:read", "View gateway monitoring", "monitoring", "read", "gateway"),
            ("gateway:ratelimits:read", "View rate limits", "ratelimits", "read", "gateway"),
            ("gateway:ratelimits:write", "Manage rate limits", "ratelimits", "write", "gateway"),

            // System-wide Administrative Permissions
            ("system:admin:full", "Full system administration", "*", "*", "system"),
            ("system:monitoring:read", "View system monitoring", "monitoring", "read", "system"),
            ("system:logs:read", "View system logs", "logs", "read", "system"),
            ("system:metrics:read", "View system metrics", "metrics", "read", "system"),
            ("system:health:read", "View system health", "health", "read", "system")
        };

        foreach (var (name, description, resource, action, service) in permissionData)
        {
            var existingPermission = await context.Permissions
                .FirstOrDefaultAsync(p => p.Name == name);

            if (existingPermission == null)
            {
                var permission = new Permission(name, description, resource, action, service);
                context.Permissions.Add(permission);
                logger.LogInformation("Added permission: {PermissionName}", permission.Name);
            }
        }
    }

    private static async Task SeedRolesAsync(IdentityDbContext context, ILogger logger)
    {
        var roles = new List<Role>
        {
            new Role("fan", "Fan", "Regular users who can purchase and use tickets", RoleType.System, true, 10),
            new Role("promoter", "Promoter", "Event organizers who can create and manage events", RoleType.System, true, 20),
            new Role("event_staff", "Event Staff", "Event staff who can verify tickets and manage check-ins", RoleType.System, true, 30),
            new Role("admin", "System Administrator", "System administrators with full access", RoleType.System, true, 100),
            new Role("support", "Support", "Customer support representatives", RoleType.System, true, 40),
            new Role("auditor", "Auditor", "Auditors who can view logs and perform compliance checks", RoleType.System, true, 50)
        };

        foreach (var role in roles)
        {
            var existingRole = await context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == role.NormalizedName);

            if (existingRole == null)
            {
                context.Roles.Add(role);
                logger.LogInformation("Added role: {RoleName}", role.Name);
            }
        }
    }

    private static async Task AssignPermissionsToRolesAsync(IdentityDbContext context, ILogger logger)
    {
        // Fan Role Permissions
        await AssignPermissionsToRole(context, "FAN", new[]
        {
            // Basic user permissions
            "identity:users:read",
            "identity:sessions:read",
            
            // Event viewing
            "events:events:read",
            "events:venues:read",
            "events:categories:read",
            
            // Ticket purchasing and management
            "ticketing:tickets:read",
            "ticketing:tickets:purchase",
            "ticketing:orders:read",
            "ticketing:payments:read",
            
            // Resale participation
            "resale:listings:read",
            "resale:listings:create",
            "resale:waitlist:join",
            
            // Notifications
            "notifications:notifications:read"
        }, logger);

        // Promoter Role Permissions
        await AssignPermissionsToRole(context, "PROMOTER", new[]
        {
            // User management (limited)
            "identity:users:read",
            "identity:sessions:read",
            
            // Full event management
            "events:events:read",
            "events:events:write",
            "events:events:delete",
            "events:venues:read",
            "events:venues:write",
            "events:categories:read",
            "events:categories:write",
            
            // Ticket and order management for their events
            "ticketing:tickets:read",
            "ticketing:orders:read",
            "ticketing:orders:write",
            "ticketing:orders:cancel",
            "ticketing:payments:read",
            "ticketing:refunds:process",
            
            // Resale management
            "resale:listings:read",
            "resale:listings:manage",
            "resale:waitlist:manage",
            "resale:transactions:read",
            
            // Event verification access
            "verification:events:access",
            "verification:checkins:read",
            
            // Notifications
            "notifications:notifications:read",
            "notifications:notifications:send",
            "notifications:templates:read",
            
            // Blockchain viewing
            "blockchain:contracts:read",
            "blockchain:transactions:read"
        }, logger);

        // Event Staff Role Permissions
        await AssignPermissionsToRole(context, "EVENT_STAFF", new[]
        {
            // Basic event information
            "events:events:read",
            "events:venues:read",
            
            // Ticket verification
            "verification:tickets:verify",
            "verification:checkins:read",
            "verification:checkins:write",
            "verification:events:access",
            
            // Basic ticket information
            "ticketing:tickets:read",
            
            // Notifications
            "notifications:notifications:read"
        }, logger);

        // System Admin Role Permissions (Full Access)
        await AssignPermissionsToRole(context, "ADMIN", new[]
        {
            "system:admin:full",
            "system:monitoring:read",
            "system:logs:read",
            "system:metrics:read",
            "system:health:read",
            
            // Identity full access
            "identity:users:read",
            "identity:users:write",
            "identity:users:delete",
            "identity:roles:read",
            "identity:roles:write",
            "identity:sessions:read",
            "identity:sessions:revoke",
            "identity:audit:read",
            
            // Gateway management
            "gateway:routes:read",
            "gateway:routes:write",
            "gateway:monitoring:read",
            "gateway:ratelimits:read",
            "gateway:ratelimits:write",
            
            // Full access to all services
            "events:events:read",
            "events:events:write",
            "events:events:delete",
            "events:venues:read",
            "events:venues:write",
            "events:categories:read",
            "events:categories:write",
            
            "ticketing:tickets:read",
            "ticketing:orders:read",
            "ticketing:orders:write",
            "ticketing:orders:cancel",
            "ticketing:payments:read",
            "ticketing:refunds:process",
            
            "resale:listings:read",
            "resale:listings:manage",
            "resale:waitlist:manage",
            "resale:transactions:read",
            
            "verification:tickets:verify",
            "verification:checkins:read",
            "verification:checkins:write",
            "verification:events:access",
            
            "notifications:notifications:read",
            "notifications:notifications:send",
            "notifications:templates:read",
            "notifications:templates:write",
            
            "blockchain:tickets:mint",
            "blockchain:tickets:burn",
            "blockchain:tickets:transfer",
            "blockchain:contracts:read",
            "blockchain:transactions:read"
        }, logger);

        // Support Role Permissions
        await AssignPermissionsToRole(context, "SUPPORT", new[]
        {
            // User support
            "identity:users:read",
            "identity:sessions:read",
            "identity:audit:read",
            
            // Event information
            "events:events:read",
            "events:venues:read",
            
            // Ticket and order support
            "ticketing:tickets:read",
            "ticketing:orders:read",
            "ticketing:orders:write",
            "ticketing:payments:read",
            "ticketing:refunds:process",
            
            // Resale support
            "resale:listings:read",
            "resale:transactions:read",
            
            // Verification support
            "verification:checkins:read",
            
            // Notifications
            "notifications:notifications:read",
            "notifications:notifications:send",
            
            // System monitoring
            "system:monitoring:read",
            "system:health:read"
        }, logger);

        // Auditor Role Permissions (Read-only)
        await AssignPermissionsToRole(context, "AUDITOR", new[]
        {
            // Identity auditing
            "identity:audit:read",
            "identity:users:read",
            "identity:sessions:read",
            
            // System monitoring
            "system:monitoring:read",
            "system:logs:read",
            "system:metrics:read",
            "system:health:read",
            
            // Read access to all transactions
            "ticketing:orders:read",
            "ticketing:payments:read",
            "resale:transactions:read",
            "blockchain:transactions:read",
            
            // Gateway monitoring
            "gateway:monitoring:read",
            "gateway:ratelimits:read"
        }, logger);
    }

    private static async Task AssignPermissionsToRole(IdentityDbContext context, string roleName, string[] permissionNames, ILogger logger)
    {
        // Use IgnoreQueryFilters to bypass soft delete filter
        var role = await context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.NormalizedName == roleName);
        if (role == null)
        {
            logger.LogWarning("Role {RoleName} not found", roleName);
            return;
        }

        foreach (var permissionName in permissionNames)
        {
            var permission = await context.Permissions.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Name == permissionName);
            if (permission == null)
            {
                logger.LogWarning("Permission {PermissionName} not found", permissionName);
                continue;
            }

            var existingRolePermission = await context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

            if (existingRolePermission == null)
            {
                var rolePermission = new RolePermission(role.Id, permission.Id);
                context.RolePermissions.Add(rolePermission);
                logger.LogInformation("Assigned permission {PermissionName} to role {RoleName}", permissionName, roleName);
            }
        }
    }
}
