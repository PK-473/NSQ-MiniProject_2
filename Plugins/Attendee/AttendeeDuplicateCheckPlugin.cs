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
            var preImage = context.PreEntityImages.Contains(M3Names.PreImageAlias)
                ? context.PreEntityImages[M3Names.PreImageAlias]
                : null;

            // On update, Target holds only changed columns: fall back to the pre-image.
            string email = M3Helpers.GetValue<string>(target, preImage, M3Names.AttEmail);
            string phone = M3Helpers.GetValue<string>(target, preImage, M3Names.AttPhone);
            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
            phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            if (email == null && phone == null) { return; }

            var query = new QueryExpression(M3Names.Attendee)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet(false)
            };

            // Match on email OR phone (Dataverse text comparison is case-insensitive).
            var either = new FilterExpression(LogicalOperator.Or);
            if (email != null) { either.AddCondition(M3Names.AttEmail, ConditionOperator.Equal, email); }
            if (phone != null) { either.AddCondition(M3Names.AttPhone, ConditionOperator.Equal, phone); }
            query.Criteria.AddFilter(either);

            if (target.Id != Guid.Empty)
            {
                query.Criteria.AddCondition(M3Names.AttendeeId, ConditionOperator.NotEqual, target.Id);
            }

            if (service.RetrieveMultiple(query).Entities.Any())
            {
                throw new InvalidPluginExecutionException("An attendee with the same email or phone number already exists.");
            }
        }
    }
}
