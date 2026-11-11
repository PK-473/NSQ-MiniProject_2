using System;
using Microsoft.Xrm.Sdk;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Session
{
    /*
     * Requirement S5:
     * A Session must have a Speaker assigned
     * before marking Status as Planned or Completed.
     */
    public class SessionSpeakerValidationPlugin : PluginBase
    {
        private const string SessionTable = "trpj_session";

        private const string SpeakerColumn = "trpj_speakerid";
        private const string StatusColumn = "trpj_session_status";

        // Replace with actual values
        private const int PlannedStatus = 738000001;
        private const int CompletedStatus = 738000002;

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "S5 validation started.");

            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "Target not found.");

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
                    "Not a Session record.");

                return;
            }

            Entity preImage = null;

            if (context.PreEntityImages.Contains("PreImage"))
            {
                preImage =
                    context.PreEntityImages["PreImage"];
            }

            OptionSetValue sessionStatus =
                target.Contains(StatusColumn)
                ? target.GetAttributeValue<OptionSetValue>(
                    StatusColumn)
                : preImage?.GetAttributeValue<OptionSetValue>(
                    StatusColumn);

            EntityReference speaker =
                target.Contains(SpeakerColumn)
                ? target.GetAttributeValue<EntityReference>(
                    SpeakerColumn)
                : preImage?.GetAttributeValue<EntityReference>(
                    SpeakerColumn);

            if (sessionStatus == null)
            {
                tracingService.Trace(
                    "Status not available.");

                return;
            }

            tracingService.Trace(
                "Status value = {0}",
                sessionStatus.Value);

            bool isPlanned =
                sessionStatus.Value == PlannedStatus;

            bool isCompleted =
                sessionStatus.Value == CompletedStatus;

            if ((isPlanned || isCompleted)
                && speaker == null)
            {
                tracingService.Trace(
                    "Blocked because Speaker is not assigned.");

                throw new InvalidPluginExecutionException(
                    "Please assign a Speaker before marking the Session as Planned or Completed.");
            }

            tracingService.Trace(
                "S5 validation completed successfully.");
        }
    }
}
