using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    /// <summary>
    /// R5 | Event Registration | Create | PreValidation | Decision D3: block only when BOTH email and phone are missing.
    /// </summary>
    public class RegistrationAttendeeContactPlugin : PluginBase
    {
        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];
            var attendeeRef = target.GetAttributeValue<EntityReference>(M3Names.RegAttendee);
            if (attendeeRef == null) { return; }

            var attendee = service.Retrieve(attendeeRef.LogicalName, attendeeRef.Id,
                new ColumnSet(M3Names.AttEmail, M3Names.AttPhone));

            var email = attendee.GetAttributeValue<string>(M3Names.AttEmail);
            var phone = attendee.GetAttributeValue<string>(M3Names.AttPhone);
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            {
                throw new InvalidPluginExecutionException(
                    "Registration is blocked because the attendee has no email and no phone number. Add at least one on the attendee record first.");
            }
        }
    }
}
