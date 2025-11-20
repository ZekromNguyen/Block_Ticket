using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SeedData");

        try
        {
            logger.LogInformation("Starting seed data initialization...");

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed in order due to dependencies
            await SeedRolesAsync(context, logger);
            await SeedScopesAsync(context, logger);
            await SeedOAuthClientsAsync(context, logger);
            await SeedDefaultUsersAsync(context, logger);

            await context.SaveChangesAsync();
            logger.LogInformation("Seed data initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(IdentityDbContext context, ILogger logger)
    {
        if (await context.Roles.AnyAsync())
        {
            logger.LogInformation("Roles already exist, skipping seed");
            return;
        }

        logger.LogInformation("Seeding roles...");

        var roles = new List<Role>
        {
            new Role("SuperAdmin", "Super Administrator", "Full system access", RoleType.System, true, 1000),
            new Role("PlatformAdmin", "Platform Administrator", "Platform management", RoleType.System, false, 800),
            new Role("Promoter", "Event Promoter", "Event and venue management", RoleType.Custom, false, 600),
            new Role("Fan", "Fan", "Basic user permissions", RoleType.Custom, false, 100),
            new Role("VenueStaff", "Venue Staff", "Ticket verification", RoleType.Custom, false, 300)
        };
        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync(); // Save roles to get Ids

        var permissions = new List<Permission>
        {
            new Permission("system:manage", "Manage System", "system", "manage", "Identity"),
            new Permission("users:manage", "Manage Users", "users", "manage", "Identity"),
            new Permission("events:manage", "Manage Events", "events", "manage", "Event"),
            new Permission("tickets:manage", "Manage Tickets", "tickets", "manage", "Ticketing"),
            new Permission("blockchain:manage", "Manage Blockchain", "blockchain", "manage", "Blockchain"),
            new Permission("payments:manage", "Manage Payments", "payments", "manage", "Payment"),
            new Permission("events:read", "Read Events", "events", "read", "Event"),
            new Permission("tickets:read", "Read Tickets", "tickets", "read", "Ticketing"),
            new Permission("payments:read", "Read Payments", "payments", "read", "Payment"),
            new Permission("events:write", "Write Events", "events", "write", "Event"),
            new Permission("venues:write", "Write Venues", "venues", "write", "Event"),
            new Permission("tickets:write", "Write Tickets", "tickets", "write", "Ticketing"),
            new Permission("blockchain:mint", "Mint Blockchain Assets", "blockchain", "mint", "Blockchain"),
            new Permission("blockchain:burn", "Burn Blockchain Assets", "blockchain", "burn", "Blockchain"),
            new Permission("tickets:purchase", "Purchase Tickets", "tickets", "purchase", "Ticketing"),
            new Permission("tickets:transfer", "Transfer Tickets", "tickets", "transfer", "Ticketing"),
            new Permission("tickets:verify", "Verify Tickets", "tickets", "verify", "Ticketing")
        };
        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync(); // Save permissions to get Ids

        var rolePermissions = new List<RolePermission>();
        var superAdmin = roles.First(r => r.Name == "SuperAdmin");
        var platformAdmin = roles.First(r => r.Name == "PlatformAdmin");
        var promoter = roles.First(r => r.Name == "Promoter");
        var fan = roles.First(r => r.Name == "Fan");
        var venueStaff = roles.First(r => r.Name == "VenueStaff");

        rolePermissions.AddRange(new[]
        {
            new RolePermission(superAdmin.Id, permissions.First(p => p.Name == "system:manage").Id),
            new RolePermission(superAdmin.Id, permissions.First(p => p.Name == "users:manage").Id),
            new RolePermission(superAdmin.Id, permissions.First(p => p.Name == "events:manage").Id),
            new RolePermission(superAdmin.Id, permissions.First(p => p.Name == "tickets:manage").Id),
            new RolePermission(superAdmin.Id, permissions.First(p => p.Name == "blockchain:manage").Id),
            new RolePermission(superAdmin.Id, permissions.First(p => p.Name == "payments:manage").Id),

            new RolePermission(platformAdmin.Id, permissions.First(p => p.Name == "users:manage").Id),
            new RolePermission(platformAdmin.Id, permissions.First(p => p.Name == "events:read").Id),
            new RolePermission(platformAdmin.Id, permissions.First(p => p.Name == "tickets:read").Id),
            new RolePermission(platformAdmin.Id, permissions.First(p => p.Name == "payments:read").Id),

            new RolePermission(promoter.Id, permissions.First(p => p.Name == "events:write").Id),
            new RolePermission(promoter.Id, permissions.First(p => p.Name == "venues:write").Id),
            new RolePermission(promoter.Id, permissions.First(p => p.Name == "tickets:write").Id),
            new RolePermission(promoter.Id, permissions.First(p => p.Name == "blockchain:mint").Id),
            new RolePermission(promoter.Id, permissions.First(p => p.Name == "blockchain:burn").Id),

            new RolePermission(fan.Id, permissions.First(p => p.Name == "events:read").Id),
            new RolePermission(fan.Id, permissions.First(p => p.Name == "tickets:purchase").Id),
            new RolePermission(fan.Id, permissions.First(p => p.Name == "tickets:transfer").Id),

            new RolePermission(venueStaff.Id, permissions.First(p => p.Name == "tickets:verify").Id),
            new RolePermission(venueStaff.Id, permissions.First(p => p.Name == "events:read").Id)
        });

        await context.Set<RolePermission>().AddRangeAsync(rolePermissions);
        logger.LogInformation("Added {Count} roles with permissions", roles.Count);
    }

    private static async Task SeedScopesAsync(IdentityDbContext context, ILogger logger)
    {
        if (await context.Scopes.AnyAsync())
        {
            logger.LogInformation("Scopes already exist, skipping seed");
            return;
        }

        logger.LogInformation("Seeding scopes...");

        var scopes = new List<Scope>
        {
            new Scope("openid", "OpenID Connect", "OpenID Connect authentication", ScopeType.Identity),
            new Scope("profile", "Profile", "User profile information", ScopeType.Identity),
            new Scope("email", "Email", "User email address", ScopeType.Identity),
            new Scope("offline_access", "Offline Access", "Refresh token access", ScopeType.Resource),
            new Scope("api:read", "API Read", "Read API access", ScopeType.Resource),
            new Scope("api:write", "API Write", "Write API access", ScopeType.Resource),
            new Scope("events:read", "Events Read", "Read events", ScopeType.Resource),
            new Scope("events:write", "Events Write", "Manage events", ScopeType.Resource),
            new Scope("tickets:read", "Tickets Read", "Read tickets", ScopeType.Resource),
            new Scope("tickets:write", "Tickets Write", "Manage tickets", ScopeType.Resource),
            new Scope("wallet:read", "Wallet Read", "Read wallet data", ScopeType.Resource),
            new Scope("wallet:write", "Wallet Write", "Manage wallet", ScopeType.Resource)
        };

        // Configure default scopes
        scopes[0].SetAsRequired(true);  // openid
        scopes[0].SetAsDefault(true);
        scopes[1].SetAsDefault(true);   // profile
        scopes[2].SetAsDefault(true);   // email
        scopes[4].SetAsDefault(true);   // api:read

        await context.Scopes.AddRangeAsync(scopes);
        logger.LogInformation("Added {Count} scopes", scopes.Count);
    }

    private static async Task SeedOAuthClientsAsync(IdentityDbContext context, ILogger logger)
    {
        if (await context.OAuthClients.AnyAsync())
        {
            logger.LogInformation("OAuth clients already exist, skipping seed");
            return;
        }

        logger.LogInformation("Seeding OAuth clients...");

        var clients = new List<OAuthClient>
        {
            new OAuthClient("blockticket-web", "BlockTicket Web App", "Web application for fans", ClientType.Public, true, false),
            new OAuthClient("blockticket-mobile", "BlockTicket Mobile App", "Mobile app for venue staff", ClientType.Public, true, false),
            new OAuthClient("blockticket-admin", "BlockTicket Admin Portal", "Admin portal", ClientType.Confidential, false, true)
        };

        // Configure clients
        foreach (var client in clients)
        {
            switch (client.ClientId)
            {
                case "blockticket-web":
                    client.AddRedirectUri("http://localhost:3000/auth/callback");
                    client.AddRedirectUri("https://blockticket.app/auth/callback");
                    client.AddScope("openid");
                    client.AddScope("profile");
                    client.AddScope("email");
                    client.AddScope("api:read");
                    client.AddScope("events:read");
                    client.AddScope("tickets:read");
                    client.AddScope("tickets:write");
                    break;
                case "blockticket-mobile":
                    client.AddRedirectUri("blockticket://auth/callback");
                    client.AddScope("openid");
                    client.AddScope("profile");
                    client.AddScope("api:read");
                    client.AddScope("tickets:read");
                    break;
                case "blockticket-admin":
                    client.SetClientSecret("admin-secret-2024");
                    client.AddRedirectUri("http://localhost:5004/signin-oidc");
                    client.AddScope("openid");
                    client.AddScope("profile");
                    client.AddScope("email");
                    client.AddScope("api:read");
                    client.AddScope("api:write");
                    break;
            }
        }

        await context.OAuthClients.AddRangeAsync(clients);
        logger.LogInformation("Added {Count} OAuth clients", clients.Count);
    }

    private static async Task SeedDefaultUsersAsync(IdentityDbContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist, skipping seed");
            return;
        }

        logger.LogInformation("Seeding default users...");

        // Get roles for assignment
        var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        var fanRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Fan");
        var promoterRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Promoter");

        var users = new List<User>();

        // Super Admin User
        if (superAdminRole != null)
        {
            var admin = new User(
                new Email("admin@blockticket.app"),
                "System",
                "Administrator",
                "hashed_password_placeholder", // In production, use proper password hashing
                UserType.Admin
            );
            admin.ConfirmEmail();
            admin.AssignRole(superAdminRole.Id, "System", null, false);
            users.Add(admin);
        }

        // Demo Fan User
        if (fanRole != null)
        {
            var fan = new User(
                new Email("demo.fan@blockticket.app"),
                "Demo",
                "Fan",
                "hashed_password_placeholder",
                UserType.Fan
            );
            fan.ConfirmEmail();
            fan.AssignRole(fanRole.Id, "System", null, false);
            users.Add(fan);
        }

        // Demo Promoter User
        if (promoterRole != null)
        {
            var promoter = new User(
                new Email("demo.promoter@blockticket.app"),
                "Demo",
                "Promoter",
                "hashed_password_placeholder",
                UserType.Promoter
            );
            promoter.ConfirmEmail();
            promoter.AssignRole(promoterRole.Id, "System", null, false);
            users.Add(promoter);
        }

        await context.Users.AddRangeAsync(users);
        logger.LogInformation("Added {Count} default users", users.Count);
    }
}
