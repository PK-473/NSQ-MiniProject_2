using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    /// <summary>
    /// E8, R3, R6 | Event Registration | Create | PreValidation | no filtering attributes
    /// Blocks registration when the event is Cancelled or Completed, or has already started.
    /// </summary>
    public class RegistrationEventStatusAndStartPlugin : PluginBase
    {
        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];
            var eventRef = target.GetAttributeValue<EntityReference>(M3Names.RegEvent);
            if (eventRef == null) { return; }

            var ev = service.Retrieve(eventRef.LogicalName, eventRef.Id,
                new ColumnSet(M3Names.EventStatus, M3Names.EventStartDate));

            var status = ev.GetAttributeValue<OptionSetValue>(M3Names.EventStatus);
            if (status != null && status.Value == M3Names.EventStatusCancelled)
            {
                throw new InvalidPluginExecutionException("Registration is not allowed because the event is Cancelled."); // E8, R3
            }
            if (status != null && status.Value == M3Names.EventStatusCompleted)
            {
                throw new InvalidPluginExecutionException("Registration is not allowed because the event is Completed."); // R3
            }

            var start = ev.GetAttributeValue<DateTime?>(M3Names.EventStartDate);
            if (start.HasValue && DateTime.UtcNow > start.Value)
            {
                throw new InvalidPluginExecutionException("Registration is closed because the event has already started."); // R6
            }
        }
    }
}
