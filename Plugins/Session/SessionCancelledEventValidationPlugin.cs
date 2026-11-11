using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Session
{
    /*
     * Requirement S11:
     * Session cannot be created
     * under a Cancelled Event.
     */
    public class SessionCancelledEventValidationPlugin : PluginBase
    {
        private const string SessionTable = "trpj_session";

        private const string EventLookupColumn = "trpj_eventid";

        private const string EventTable = "trpj_event";

        private const string EventStatusColumn = "trpj_eventstatus";

        // Replace with actual value
        private const int CancelledStatus = 100000003;

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "S11 validation started.");

            if (!context.InputParameters.Contains("Target"))
            {
                return;
            }

            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                return;
            }

            EntityReference eventReference =
                target.GetAttributeValue<EntityReference>(
                    EventLookupColumn);

            if (eventReference == null)
            {
                return;
            }

            Entity eventRecord =
                service.Retrieve(
                    EventTable,
                    eventReference.Id,
                    new ColumnSet(EventStatusColumn));

            OptionSetValue status =
                eventRecord.GetAttributeValue<OptionSetValue>(
                    EventStatusColumn);

            if (status != null &&
                status.Value == CancelledStatus)
            {
                tracingService.Trace(
                    "Cancelled Event detected.");

                throw new InvalidPluginExecutionException(
                    "Cannot create a Session under a Cancelled Event.");
            }

            tracingService.Trace(
                "S11 completed successfully.");
        }
    }
}
