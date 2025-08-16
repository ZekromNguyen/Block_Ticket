using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaToIdentityCustom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create identity schema if it doesn't exist
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS identity;");

            // List of tables to move from BlockTicket_Identity to identity schema
            var tables = new[]
            {
                "UserSessions", "Users", "UserRoles", "SuspiciousActivities", "SecurityEvents",
                "Scopes", "Roles", "ReferenceTokens", "Permissions", "OpenIddictTokens",
                "OpenIddictScopes", "OpenIddictAuthorizations", "OpenIddictApplications",
                "OAuthClients", "MfaDevices", "AuditLogs", "AccountLockouts"
            };

            foreach (var table in tables)
            {
                // Check if table exists in BlockTicket_Identity schema and move it
                migrationBuilder.Sql($@"
                    DO $$
                    BEGIN
                        -- Check if table exists in BlockTicket_Identity schema
                        IF EXISTS (
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_schema = 'BlockTicket_Identity' 
                            AND table_name = '{table}'
                        ) THEN
                            -- Check if table doesn't already exist in identity schema
                            IF NOT EXISTS (
                                SELECT 1 FROM information_schema.tables 
                                WHERE table_schema = 'identity' 
                                AND table_name = '{table}'
                            ) THEN
                                -- Move the table to identity schema
                                EXECUTE 'ALTER TABLE ""BlockTicket_Identity"".""{table}"" SET SCHEMA identity';
                            ELSE
                                -- If table exists in both schemas, drop from BlockTicket_Identity
                                EXECUTE 'DROP TABLE IF EXISTS ""BlockTicket_Identity"".""{table}"" CASCADE';
                            END IF;
                        END IF;
                    END $$;
                ");
            }

            // Drop BlockTicket_Identity schema if it's empty
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Check if BlockTicket_Identity schema exists and is empty
                    IF EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'BlockTicket_Identity') THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_schema = 'BlockTicket_Identity'
                        ) THEN
                            DROP SCHEMA ""BlockTicket_Identity"";
                        END IF;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create BlockTicket_Identity schema if it doesn't exist
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS \"BlockTicket_Identity\";");

            // List of tables to move back from identity to BlockTicket_Identity schema
            var tables = new[]
            {
                "UserSessions", "Users", "UserRoles", "SuspiciousActivities", "SecurityEvents",
                "Scopes", "Roles", "ReferenceTokens", "Permissions", "OpenIddictTokens",
                "OpenIddictScopes", "OpenIddictAuthorizations", "OpenIddictApplications",
                "OAuthClients", "MfaDevices", "AuditLogs", "AccountLockouts"
            };

            foreach (var table in tables)
            {
                // Move tables back to BlockTicket_Identity schema
                migrationBuilder.Sql($@"
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_schema = 'identity' 
                            AND table_name = '{table}'
                        ) THEN
                            EXECUTE 'ALTER TABLE identity.""{table}"" SET SCHEMA ""BlockTicket_Identity""';
                        END IF;
                    END $$;
                ");
            }
        }
    }
}
