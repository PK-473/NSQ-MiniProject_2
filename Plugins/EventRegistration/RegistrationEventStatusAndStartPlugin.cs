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
            var eventRef = target.GetAttributeValue<EntityReference>(LogicalNames.RegEvent);
            if (eventRef == null) { return; }

            var ev = service.Retrieve(eventRef.LogicalName, eventRef.Id,
                new ColumnSet(LogicalNames.EventStatus, LogicalNames.EventStartDate));

            var status = ev.GetAttributeValue<OptionSetValue>(LogicalNames.EventStatus);
            if (status != null && status.Value == LogicalNames.EventStatusCancelled)
            {
                throw new InvalidPluginExecutionException("Registration is not allowed because the event is Cancelled."); // E8, R3
            }
            if (status != null && status.Value == LogicalNames.EventStatusCompleted)
            {
                throw new InvalidPluginExecutionException("Registration is not allowed because the event is Completed."); // R3
            }

            var start = ev.GetAttributeValue<DateTime?>(LogicalNames.EventStartDate);
            if (start.HasValue && DateTime.UtcNow > start.Value)
            {
                throw new InvalidPluginExecutionException("Registration is closed because the event has already started."); // R6
            }
        }
    }
}
