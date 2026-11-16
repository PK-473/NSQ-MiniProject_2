using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    /// <summary>
    /// R4 | Event Registration | Create and Update | PreValidation
    /// Filtering: eventid, attendeeid | Pre-image (alias PreImage): eventid, attendeeid
    /// </summary>
    public class RegistrationDuplicateAttendeePlugin : PluginBase
    {
        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];
            var preImage = RegistrationHelper.GetPreImage(context);

            var eventRef = PluginHelpers.GetValue<EntityReference>(target, preImage, LogicalNames.RegEvent);
            var attendeeRef = PluginHelpers.GetValue<EntityReference>(target, preImage, LogicalNames.RegAttendee);
            if (eventRef == null || attendeeRef == null) { return; }

            var query = new QueryExpression(LogicalNames.Registration)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet(false)
            };
            query.Criteria.AddCondition(LogicalNames.RegEvent, ConditionOperator.Equal, eventRef.Id);
            query.Criteria.AddCondition(LogicalNames.RegAttendee, ConditionOperator.Equal, attendeeRef.Id);
            // A cancelled registration does not count, so the attendee can register again.
            query.Criteria.AddCondition(LogicalNames.RegStatus, ConditionOperator.NotEqual, LogicalNames.RegStatusCancelled);
            if (target.Id != Guid.Empty)
            {
                query.Criteria.AddCondition(LogicalNames.RegistrationId, ConditionOperator.NotEqual, target.Id);
            }

            if (service.RetrieveMultiple(query).Entities.Any())
            {
                throw new InvalidPluginExecutionException("This attendee is already registered for this event.");
            }
        }
    }
}
