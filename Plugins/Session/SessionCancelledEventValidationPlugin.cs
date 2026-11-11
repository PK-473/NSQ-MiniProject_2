using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Session
{
    /*
     * Requirements:
     *
     * S11:
     * Session cannot be created under a Cancelled Event.
     *
     * E10:
     * Once Event Status becomes Completed or Cancelled,
     * Sessions cannot be created or modified.
     */
    public class SessionCancelledEventValidationPlugin : PluginBase
    {
        private const string SessionTable = "trpj_session";

        private const string EventLookupColumn = "trpj_eventid";

        private const string EventTable = "trpj_event";

        private const string EventStatusColumn = "trpj_eventstatus";

        private const int CompletedStatus = 100000002;

        private const int CancelledStatus = 100000003;

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "E10/S11 validation started. Message: {0}",
                context.MessageName);

            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "Target parameter not found.");

                return;
            }

            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "Target is null.");

                return;
            }

            if (!target.LogicalName.Equals(
                    SessionTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "Target is not Session.");

                return;
            }

            Entity preImage = null;

            if (context.PreEntityImages.Contains("PreImage"))
            {
                preImage =
                    context.PreEntityImages["PreImage"];
            }

            EntityReference eventReference =
                target.Contains(EventLookupColumn)
                ? target.GetAttributeValue<EntityReference>(
                    EventLookupColumn)
                : preImage?.GetAttributeValue<EntityReference>(
                    EventLookupColumn);

            if (eventReference == null)
            {
                tracingService.Trace(
                    "Event lookup not found.");

                return;
            }

            tracingService.Trace(
                "Retrieving Event record.");

            Entity eventRecord =
                service.Retrieve(
                    EventTable,
                    eventReference.Id,
                    new ColumnSet(
                        EventStatusColumn));

            OptionSetValue eventStatus =
                eventRecord.GetAttributeValue<OptionSetValue>(
                    EventStatusColumn);

            if (eventStatus == null)
            {
                tracingService.Trace(
                    "Event Status is null.");

                return;
            }

            tracingService.Trace(
                "Event Status Value: {0}",
                eventStatus.Value);

            bool isCompleted =
                eventStatus.Value == CompletedStatus;

            bool isCancelled =
                eventStatus.Value == CancelledStatus;

            if (isCompleted || isCancelled)
            {
                tracingService.Trace(
                    "Blocked because Event is Completed or Cancelled.");

                throw new InvalidPluginExecutionException(
                    "Sessions cannot be created or modified because the selected Event is Completed or Cancelled.");
            }

            tracingService.Trace(
                "E10/S11 validation completed successfully.");
        }
    }
}
