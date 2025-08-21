using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Execute the RLS SQL script
            var rlsScript = @"
-- Enable RLS on all tenant-scoped tables
ALTER TABLE event.events ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.venues ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.ticket_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.pricing_rules ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.allocations ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.reservations ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.reservation_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.event_series ENABLE ROW LEVEL SECURITY;
ALTER TABLE event.seats ENABLE ROW LEVEL SECURITY;

-- Create function to get current organization context
CREATE OR REPLACE FUNCTION event.current_organization_id()
RETURNS uuid AS $$
BEGIN
    RETURN COALESCE(
        NULLIF(current_setting('app.current_organization_id', true), '')::uuid,
        '00000000-0000-0000-0000-000000000000'::uuid
    );
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- Create RLS policies for events table
CREATE POLICY events_tenant_isolation ON event.events
    FOR ALL
    TO PUBLIC
    USING (organization_id = event.current_organization_id());

-- Create RLS policies for venues table
CREATE POLICY venues_tenant_isolation ON event.venues
    FOR ALL
    TO PUBLIC
    USING (organization_id = event.current_organization_id());

-- Create RLS policies for ticket_types table
CREATE POLICY ticket_types_tenant_isolation ON event.ticket_types
    FOR ALL
    TO PUBLIC
    USING (
        EXISTS (
            SELECT 1 FROM event.events e 
            WHERE e.id = ticket_types.event_id 
            AND e.organization_id = event.current_organization_id()
        )
    );

-- Create RLS policies for pricing_rules table
CREATE POLICY pricing_rules_tenant_isolation ON event.pricing_rules
    FOR ALL
    TO PUBLIC
    USING (
        EXISTS (
            SELECT 1 FROM event.events e 
            WHERE e.id = pricing_rules.event_id 
            AND e.organization_id = event.current_organization_id()
        )
    );

-- Create RLS policies for allocations table
CREATE POLICY allocations_tenant_isolation ON event.allocations
    FOR ALL
    TO PUBLIC
    USING (
        EXISTS (
            SELECT 1 FROM event.events e 
            WHERE e.id = allocations.event_id 
            AND e.organization_id = event.current_organization_id()
        )
    );

-- Create RLS policies for reservations table
CREATE POLICY reservations_tenant_isolation ON event.reservations
    FOR ALL
    TO PUBLIC
    USING (
        EXISTS (
            SELECT 1 FROM event.events e 
            WHERE e.id = reservations.event_id 
            AND e.organization_id = event.current_organization_id()
        )
    );

-- Create RLS policies for reservation_items table
CREATE POLICY reservation_items_tenant_isolation ON event.reservation_items
    FOR ALL
    TO PUBLIC
    USING (
        EXISTS (
            SELECT 1 FROM event.reservations r
            JOIN event.events e ON e.id = r.event_id
            WHERE r.id = reservation_items.reservation_id 
            AND e.organization_id = event.current_organization_id()
        )
    );

-- Create RLS policies for event_series table
CREATE POLICY event_series_tenant_isolation ON event.event_series
    FOR ALL
    TO PUBLIC
    USING (organization_id = event.current_organization_id());

-- Create RLS policies for seats table
CREATE POLICY seats_tenant_isolation ON event.seats
    FOR ALL
    TO PUBLIC
    USING (
        EXISTS (
            SELECT 1 FROM event.venues v 
            WHERE v.id = seats.venue_id 
            AND v.organization_id = event.current_organization_id()
        )
    );

-- Create indexes on organization_id columns for performance
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_events_organization_id 
    ON event.events (organization_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_venues_organization_id 
    ON event.venues (organization_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_event_series_organization_id 
    ON event.event_series (organization_id);

-- Create composite indexes for common query patterns
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_events_org_status_date 
    ON event.events (organization_id, status, event_date);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_events_org_slug 
    ON event.events (organization_id, slug);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_venues_org_name 
    ON event.venues (organization_id, name);

-- Grant necessary permissions
GRANT EXECUTE ON FUNCTION event.current_organization_id() TO PUBLIC;

-- Create comments for documentation
COMMENT ON FUNCTION event.current_organization_id() IS 'Returns the current organization ID from session context for RLS enforcement';
";

            migrationBuilder.Sql(rlsScript);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Disable RLS and drop policies
            var rollbackScript = @"
-- Drop RLS policies
DROP POLICY IF EXISTS events_tenant_isolation ON event.events;
DROP POLICY IF EXISTS venues_tenant_isolation ON event.venues;
DROP POLICY IF EXISTS ticket_types_tenant_isolation ON event.ticket_types;
DROP POLICY IF EXISTS pricing_rules_tenant_isolation ON event.pricing_rules;
DROP POLICY IF EXISTS allocations_tenant_isolation ON event.allocations;
DROP POLICY IF EXISTS reservations_tenant_isolation ON event.reservations;
DROP POLICY IF EXISTS reservation_items_tenant_isolation ON event.reservation_items;
DROP POLICY IF EXISTS event_series_tenant_isolation ON event.event_series;
DROP POLICY IF EXISTS seats_tenant_isolation ON event.seats;

-- Disable RLS on tables
ALTER TABLE event.events DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.venues DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.ticket_types DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.pricing_rules DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.allocations DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.reservations DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.reservation_items DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.event_series DISABLE ROW LEVEL SECURITY;
ALTER TABLE event.seats DISABLE ROW LEVEL SECURITY;

-- Drop function
DROP FUNCTION IF EXISTS event.current_organization_id();

-- Drop indexes
DROP INDEX IF EXISTS event.idx_events_organization_id;
DROP INDEX IF EXISTS event.idx_venues_organization_id;
DROP INDEX IF EXISTS event.idx_event_series_organization_id;
DROP INDEX IF EXISTS event.idx_events_org_status_date;
DROP INDEX IF EXISTS event.idx_events_org_slug;
DROP INDEX IF EXISTS event.idx_venues_org_name;
";

            migrationBuilder.Sql(rollbackScript);
        }
    }
}
