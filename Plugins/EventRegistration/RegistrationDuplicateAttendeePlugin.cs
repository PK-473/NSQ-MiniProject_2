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

            var eventRef = M3Helpers.GetValue<EntityReference>(target, preImage, M3Names.RegEvent);
            var attendeeRef = M3Helpers.GetValue<EntityReference>(target, preImage, M3Names.RegAttendee);
            if (eventRef == null || attendeeRef == null) { return; }

            var query = new QueryExpression(M3Names.Registration)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet(false)
            };
            query.Criteria.AddCondition(M3Names.RegEvent, ConditionOperator.Equal, eventRef.Id);
            query.Criteria.AddCondition(M3Names.RegAttendee, ConditionOperator.Equal, attendeeRef.Id);
            // A cancelled registration does not count, so the attendee can register again.
            query.Criteria.AddCondition(M3Names.RegStatus, ConditionOperator.NotEqual, M3Names.RegStatusCancelled);
            if (target.Id != Guid.Empty)
            {
                query.Criteria.AddCondition(M3Names.RegistrationId, ConditionOperator.NotEqual, target.Id);
            }

            if (service.RetrieveMultiple(query).Entities.Any())
            {
                throw new InvalidPluginExecutionException("This attendee is already registered for this event.");
            }
        }
    }
}
