# Approval Workflows & Dual-Control Feature

## Overview

The Approval Workflows feature implements comprehensive dual-control mechanisms for sensitive operations within the Event Service. This enterprise-grade solution ensures proper governance, security, and compliance for critical business operations through mandatory approvals from authorized personnel.

## Features

### Core Functionality

#### 🔐 Dual-Control Security
- **Multi-Approver Requirements**: Configurable 1-N approvals based on operation risk
- **Role-Based Authorization**: Restrict approvals to specific organizational roles
- **Sequential/Parallel Approval**: Flexible approval workflows
- **Approval Segregation**: Prevent self-approval and enforce independence

#### 📋 Operation Coverage
- **Event Operations**: Publish, cancel, price changes, date modifications
- **Venue Operations**: Modifications, deactivations, seat map changes
- **Financial Operations**: Bulk refunds, pricing rule changes
- **Administrative Operations**: Security role changes, data exports
- **Compliance Operations**: Administrative overrides, reservation overrides

#### ⚡ Intelligent Processing
- **Auto-Approval**: Low-risk operations with configurable conditions
- **Risk Assessment**: Automatic risk level determination
- **Escalation Management**: Time-based escalation to senior roles
- **Expiration Handling**: Automatic workflow expiration with cleanup

#### 📊 Comprehensive Auditing
- **Complete Audit Trail**: Every action, decision, and modification logged
- **Security Context**: IP addresses, user agents, timestamps recorded
- **Compliance Reporting**: Full audit history for regulatory requirements
- **Decision Tracking**: Detailed approval/rejection reasoning

### Workflow States

#### Workflow Lifecycle
```
Draft → Pending → Under Review → Approved/Rejected/Expired
```

#### State Transitions
- **Pending**: Initial state waiting for approvers
- **Under Review**: Partial approvals received, waiting for more
- **Approved**: All required approvals received, ready for execution
- **Rejected**: Any approver rejected, workflow terminated
- **Expired**: Exceeded time limit without completion
- **Cancelled**: Manually cancelled by requester or admin

### Risk-Based Controls

#### Risk Levels
- **Low**: Auto-approval eligible, single approval required
- **Medium**: Standard dual approval, moderate escalation
- **High**: Enhanced approval requirements, rapid escalation
- **Critical**: Maximum approvals, immediate escalation

#### Dynamic Risk Assessment
- **Operation Type**: Inherent risk based on operation category
- **Impact Analysis**: Financial and operational impact evaluation
- **Timing Constraints**: Time-sensitive operations get priority
- **Historical Context**: Previous approval patterns consideration

## API Endpoints

### Workflow Management
```
POST   /api/v1/approvals                     - Create approval workflow
GET    /api/v1/approvals/{id}                - Get workflow details
GET    /api/v1/approvals                     - List workflows with filtering
GET    /api/v1/approvals/pending             - Get pending approvals for user
POST   /api/v1/approvals/{id}/approve        - Submit approval decision
POST   /api/v1/approvals/{id}/execute        - Execute approved workflow
POST   /api/v1/approvals/{id}/cancel         - Cancel pending workflow
GET    /api/v1/approvals/{id}/audit          - Get workflow audit history
GET    /api/v1/approvals/statistics          - Get approval statistics
POST   /api/v1/approvals/check-requirement   - Check if operation needs approval
POST   /api/v1/approvals/validate-operation  - Validate operation before submission
```

### Template Management
```
POST   /api/v1/approval-templates            - Create/update approval template
GET    /api/v1/approval-templates/by-operation/{type} - Get template by operation
GET    /api/v1/approval-templates/operation-types - Get available operation types
POST   /api/v1/approval-templates/create-defaults - Create default templates
POST   /api/v1/approval-templates/test       - Test template configuration
```

## Workflow Examples

### High-Risk Event Cancellation
```json
{
  "operationType": "EventCancel",
  "entityType": "Event",
  "entityId": "123e4567-e89b-12d3-a456-426614174000",
  "operationDescription": "Cancel sold-out concert due to venue issues",
  "businessJustification": "Venue structural issues require immediate cancellation for safety",
  "expectedImpact": "Refund 5,000 tickets, estimated loss $500,000",
  "riskLevel": "High",
  "priority": "Urgent",
  "operationData": {
    "reason": "Venue safety concerns",
    "refundPolicy": "full",
    "notificationRequired": true
  }
}
```

### Response
```json
{
  "success": true,
  "workflowId": "456e7890-e89b-12d3-a456-426614174001",
  "status": "Pending",
  "messages": ["Approval workflow created successfully"],
  "metadata": {
    "requiredApprovals": 2,
    "expiresAt": "2024-01-15T12:00:00Z",
    "escalationTrigger": "24 hours"
  }
}
```

### Approval Decision Submission
```json
{
  "decision": "Approved",
  "comments": "Venue safety assessment confirms structural issues. Cancellation approved for customer safety.",
  "decisionMetadata": {
    "reviewedDocuments": ["safety-report.pdf", "structural-assessment.pdf"],
    "consultedWith": ["venue-manager", "safety-inspector"]
  }
}
```

## Template Configuration

### Event Publication Template
```json
{
  "name": "Event Publication Approval",
  "description": "Required approval for publishing events to public",
  "operationType": "EventPublish",
  "requiredApprovals": 1,
  "requiredRoles": ["Event Manager", "Marketing Manager"],
  "defaultRiskLevel": "Medium",
  "defaultExpirationDays": 3,
  "autoApprovalConditions": {
    "maxImpactValue": 10000,
    "timeConstraints": {
      "minTimeBeforeEvent": "P7D",
      "allowedDaysOfWeek": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
      "allowedTimeWindow": {
        "startTime": "09:00:00",
        "endTime": "17:00:00",
        "timeZone": "America/New_York"
      }
    },
    "criteria": {
      "eventCapacity": { "max": 500 },
      "ticketPriceRange": { "max": 100 }
    }
  },
  "escalationRules": [
    {
      "triggerAfter": "P1D",
      "escalateToRoles": ["Senior Manager"],
      "escalationMessage": "Event publication approval overdue - requires senior management attention"
    }
  ]
}
```

### Bulk Refund Template
```json
{
  "name": "Bulk Refund Approval",
  "description": "High-value bulk refund operations requiring dual approval",
  "operationType": "BulkRefund",
  "requiredApprovals": 2,
  "requiredRoles": ["Finance Manager", "Senior Manager"],
  "defaultRiskLevel": "High",
  "defaultExpirationDays": 2,
  "escalationRules": [
    {
      "triggerAfter": "PT12H",
      "escalateToRoles": ["Director", "CFO"],
      "escalationMessage": "URGENT: High-value refund approval required within 12 hours"
    }
  ]
}
```

## Security & Compliance

### Security Controls
- **Authentication Required**: All approval operations require valid JWT tokens
- **Role-Based Access**: Approvals restricted to authorized roles only
- **Audit Logging**: Complete security event logging for compliance
- **IP Tracking**: Source IP addresses recorded for all decisions
- **Session Security**: User agent and session context captured

### Compliance Features
- **SOX Compliance**: Segregation of duties for financial operations
- **GDPR Support**: Data export approvals and audit trails
- **PCI DSS**: Payment-related operation controls
- **Internal Controls**: Customizable approval matrices

### Anti-Fraud Measures
- **Self-Approval Prevention**: Users cannot approve their own requests
- **Collusion Detection**: Pattern analysis for suspicious approval behavior
- **Time-Based Controls**: Prevents approval rushing through time constraints
- **Geographic Validation**: IP-based location verification for high-risk operations

## Configuration

### Risk Level Mapping
```json
{
  "EventPublish": "Medium",
  "EventCancel": "High", 
  "EventPriceChange": "Medium",
  "EventDateChange": "High",
  "VenueDeactivation": "High",
  "BulkRefund": "High",
  "AdminOverride": "Critical",
  "SecurityRoleChange": "Critical"
}
```

### Default Approval Requirements
```json
{
  "Low": 1,
  "Medium": 1,
  "High": 2,
  "Critical": 3
}
```

### Expiration Policies
```json
{
  "Low": "P3D",      // 3 days
  "Medium": "P7D",   // 7 days  
  "High": "P3D",     // 3 days
  "Critical": "P1D"  // 1 day
}
```

## Monitoring & Analytics

### Key Metrics
- **Approval Velocity**: Average time from request to approval
- **Rejection Rate**: Percentage of workflows rejected
- **Escalation Frequency**: How often escalations occur
- **Auto-Approval Rate**: Percentage of operations auto-approved
- **Compliance Score**: Adherence to approval policies

### Dashboard Views
- **Pending Approvals**: Real-time view of pending workflows
- **Approval Trends**: Historical approval patterns and trends
- **Risk Distribution**: Breakdown by risk levels and operation types
- **User Activity**: Individual approver performance and workload
- **Compliance Report**: Detailed audit trail for compliance teams

### Alerting
- **Overdue Approvals**: Notifications for approaching expiration
- **High-Risk Operations**: Immediate alerts for critical workflows
- **Escalation Triggers**: Automated escalation notifications
- **Compliance Violations**: Alerts for policy violations

## Integration Points

### Event Operations
```csharp
// Before publishing an event
var requiresApproval = await _approvalService.RequiresApprovalAsync(
    ApprovalOperationType.EventPublish, organizationId);

if (requiresApproval)
{
    var workflowRequest = new CreateApprovalWorkflowRequest
    {
        OperationType = ApprovalOperationType.EventPublish,
        EntityType = "Event",
        EntityId = eventId,
        OperationDescription = "Publish concert event",
        BusinessJustification = "Marketing campaign launch",
        OperationData = publishRequest
    };
    
    var result = await _approvalService.CreateWorkflowAsync(
        workflowRequest, userId, organizationId);
    
    return AcceptedAt("GetWorkflow", new { id = result.WorkflowId });
}

// Direct execution for non-approval operations
await ExecuteEventPublishAsync(eventId);
```

### Validation Integration
```csharp
public async Task<IActionResult> CreatePricingRule(CreatePricingRuleRequest request)
{
    // Validate operation before creating approval workflow
    var validation = await _operationExecutor.ValidateOperationAsync(
        ApprovalOperationType.PricingRuleCreation,
        "PricingRule",
        Guid.Empty,
        request);
    
    if (!validation.IsValid)
    {
        return BadRequest(validation.ValidationErrors);
    }
    
    // Create approval workflow
    return await CreateApprovalWorkflow(request);
}
```

## Error Handling

### Common Error Scenarios
- **Insufficient Permissions**: User lacks approval rights for operation
- **Duplicate Workflows**: Entity already has pending approval
- **Expired Sessions**: Approval session timeout handling
- **Validation Failures**: Operation data validation errors
- **Template Missing**: No approval template configured for operation

### Error Response Format
```json
{
  "title": "Approval workflow creation failed",
  "detail": "There are already pending approval workflows for this entity",
  "status": 409,
  "type": "https://api.eventservice.com/errors/duplicate-workflow",
  "instance": "/api/v1/approvals",
  "extensions": {
    "workflowId": "existing-workflow-id",
    "entityType": "Event",
    "entityId": "123e4567-e89b-12d3-a456-426614174000"
  }
}
```

## Best Practices

### Template Design
1. **Risk-Appropriate Controls**: Match approval requirements to actual risk
2. **Clear Descriptions**: Provide detailed operation descriptions
3. **Reasonable Timeframes**: Set realistic expiration times
4. **Escalation Planning**: Define clear escalation paths
5. **Auto-Approval Criteria**: Use for low-risk, routine operations

### Operational Guidelines
1. **Timely Reviews**: Encourage prompt approval decisions
2. **Detailed Justifications**: Require comprehensive business reasons
3. **Regular Audits**: Review approval patterns and effectiveness
4. **Template Maintenance**: Keep approval templates current
5. **Training Programs**: Ensure approvers understand responsibilities

### Security Considerations
1. **Least Privilege**: Grant minimal necessary approval rights
2. **Regular Reviews**: Audit approver permissions quarterly
3. **Segregation Enforcement**: Prevent conflicts of interest
4. **Audit Retention**: Maintain audit logs per compliance requirements
5. **Incident Response**: Have procedures for approval system breaches

## Future Enhancements

### Planned Features
- **AI-Powered Risk Assessment**: Machine learning for dynamic risk evaluation
- **Predictive Analytics**: Forecast approval bottlenecks and delays
- **Mobile Approval App**: Native mobile application for approvers
- **Integration APIs**: Enhanced third-party system integration
- **Advanced Reporting**: Power BI/Tableau integration for analytics

### Advanced Workflows
- **Conditional Approval**: Complex business rule-based routing
- **Parallel Paths**: Multiple simultaneous approval tracks
- **Delegation Support**: Temporary approval delegation
- **Batch Processing**: Bulk approval capabilities
- **Real-time Collaboration**: Live approval discussions and comments

## Support

For technical support, configuration assistance, or feature requests related to Approval Workflows, please contact the development team or create an issue in the project repository.

The Approval Workflows system provides enterprise-grade governance and control, ensuring that all sensitive operations undergo proper review and authorization while maintaining operational efficiency and compliance requirements.
