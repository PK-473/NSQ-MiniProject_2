using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Attendee
{
    /// <summary>
    /// A1 | Attendee | Create and Update | PreValidation
    /// Filtering: email, phone | Pre-image (alias PreImage): email, phone
    /// Blocks a second attendee with the same Email (case-insensitive) or Phone.
    /// </summary>
    public class AttendeeDuplicateCheckPlugin : PluginBase
    {
        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];
            var preImage = context.PreEntityImages.Contains(LogicalNames.PreImageAlias)
                ? context.PreEntityImages[LogicalNames.PreImageAlias]
                : null;

            // On update, Target holds only changed columns: fall back to the pre-image.
            string email = PluginHelpers.GetValue<string>(target, preImage, LogicalNames.AttEmail);
            string phone = PluginHelpers.GetValue<string>(target, preImage, LogicalNames.AttPhone);
            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
            phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            if (email == null && phone == null) { return; }

            var query = new QueryExpression(LogicalNames.Attendee)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet(false)
            };

            // Match on email OR phone (Dataverse text comparison is case-insensitive).
            var either = new FilterExpression(LogicalOperator.Or);
            if (email != null) { either.AddCondition(LogicalNames.AttEmail, ConditionOperator.Equal, email); }
            if (phone != null) { either.AddCondition(LogicalNames.AttPhone, ConditionOperator.Equal, phone); }
            query.Criteria.AddFilter(either);

            if (target.Id != Guid.Empty)
            {
                query.Criteria.AddCondition(LogicalNames.AttendeeId, ConditionOperator.NotEqual, target.Id);
            }

            if (service.RetrieveMultiple(query).Entities.Any())
            {
                throw new InvalidPluginExecutionException("An attendee with the same email or phone number already exists.");
            }
        }
    }
}
