-- =====================================================
-- Row-Level Security (RLS) Implementation for Event Service
-- =====================================================

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
    -- Get organization ID from session variable set by application
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

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_reservations_org_user_status 
    ON event.reservations (
        (SELECT organization_id FROM event.events WHERE id = event_id),
        user_id, 
        status
    );

-- Create function to validate organization access
CREATE OR REPLACE FUNCTION event.validate_organization_access(target_org_id uuid)
RETURNS boolean AS $$
BEGIN
    -- Validate that the current user has access to the target organization
    RETURN target_org_id = event.current_organization_id();
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- Create audit trigger function for RLS violations
CREATE OR REPLACE FUNCTION event.audit_rls_violation()
RETURNS trigger AS $$
BEGIN
    -- Log RLS policy violations for security monitoring
    INSERT INTO event.audit_logs (
        id,
        table_name,
        operation_type,
        entity_id,
        user_id,
        organization_id,
        old_values,
        new_values,
        timestamp,
        correlation_id
    ) VALUES (
        gen_random_uuid(),
        TG_TABLE_NAME,
        TG_OP,
        COALESCE(NEW.id, OLD.id),
        current_setting('app.current_user_id', true)::uuid,
        event.current_organization_id(),
        CASE WHEN TG_OP = 'DELETE' THEN row_to_json(OLD) ELSE NULL END,
        CASE WHEN TG_OP IN ('INSERT', 'UPDATE') THEN row_to_json(NEW) ELSE NULL END,
        NOW(),
        current_setting('app.correlation_id', true)
    );
    
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- Create security monitoring views
CREATE OR REPLACE VIEW event.rls_policy_status AS
SELECT 
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd,
    qual,
    with_check
FROM pg_policies 
WHERE schemaname = 'event'
ORDER BY tablename, policyname;

-- Create function to test RLS policies
CREATE OR REPLACE FUNCTION event.test_rls_isolation(test_org_id uuid)
RETURNS TABLE(
    table_name text,
    total_rows bigint,
    accessible_rows bigint,
    isolation_effective boolean
) AS $$
BEGIN
    -- Set test organization context
    PERFORM set_config('app.current_organization_id', test_org_id::text, true);
    
    -- Test events table
    RETURN QUERY
    SELECT 
        'events'::text,
        (SELECT count(*) FROM event.events)::bigint,
        (SELECT count(*) FROM event.events WHERE organization_id = test_org_id)::bigint,
        (SELECT count(*) FROM event.events) = (SELECT count(*) FROM event.events WHERE organization_id = test_org_id);
    
    -- Test venues table
    RETURN QUERY
    SELECT 
        'venues'::text,
        (SELECT count(*) FROM event.venues)::bigint,
        (SELECT count(*) FROM event.venues WHERE organization_id = test_org_id)::bigint,
        (SELECT count(*) FROM event.venues) = (SELECT count(*) FROM event.venues WHERE organization_id = test_org_id);
    
    -- Reset session
    PERFORM set_config('app.current_organization_id', '', true);
END;
$$ LANGUAGE plpgsql;

-- Grant necessary permissions
GRANT EXECUTE ON FUNCTION event.current_organization_id() TO PUBLIC;
GRANT EXECUTE ON FUNCTION event.validate_organization_access(uuid) TO PUBLIC;
GRANT EXECUTE ON FUNCTION event.test_rls_isolation(uuid) TO PUBLIC;
GRANT SELECT ON event.rls_policy_status TO PUBLIC;

-- Create comments for documentation
COMMENT ON FUNCTION event.current_organization_id() IS 'Returns the current organization ID from session context for RLS enforcement';
COMMENT ON FUNCTION event.validate_organization_access(uuid) IS 'Validates that the current user has access to the specified organization';
COMMENT ON FUNCTION event.test_rls_isolation(uuid) IS 'Tests RLS policy effectiveness for a given organization';

-- Enable query plan caching for RLS functions
ALTER FUNCTION event.current_organization_id() SET plan_cache_mode = force_generic_plan;
ALTER FUNCTION event.validate_organization_access(uuid) SET plan_cache_mode = force_generic_plan;
