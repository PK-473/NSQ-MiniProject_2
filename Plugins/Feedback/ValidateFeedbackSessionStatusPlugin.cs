using System;
using Microsoft.Xrm.Sdk;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Feedback
{
    /*
     * Requirement F8:
     * Feedback can be created only when the selected
     * Session has Session Status = Completed.
     */
    public class ValidateFeedbackSessionStatusPlugin : PluginBase
    {
        // Logical names used by this plug-in
        private const string FeedbackTable = "trpj_feedback";
        private const string SessionTable = "trpj_session";
        private const string SessionLookupColumn = "trpj_session";
        private const string SessionStatusColumn = "trpj_session_status";

        // Completed choice value from the Session Status column
        private const int CompletedStatusValue = 738000002;

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "F8 started. Message: {0}, Stage: {1}, Mode: {2}",
                context.MessageName,
                context.Stage,
                context.Mode);

            // F8 should run only when Feedback is created.
            if (!context.MessageName.Equals(
                    "Create",
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "F8 stopped because the message is not Create.");

                return;
            }

            // Confirm that Target is available.
            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "F8 stopped because Target is missing.");

                return;
            }

            // Convert Target into an Entity.
            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "F8 stopped because Target is not an Entity.");

                return;
            }

            tracingService.Trace(
                "Target received. Logical name: {0}",
                target.LogicalName);

            // Confirm that Target belongs to the Feedback table.
            if (!target.LogicalName.Equals(
                    FeedbackTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "F8 stopped because Target is not Feedback.");

                return;
            }

            tracingService.Trace(
                "Feedback table confirmed.");

            // Get the selected Session lookup from Feedback.
            EntityReference sessionReference =
                target.GetAttributeValue<EntityReference>(
                    SessionLookupColumn);

            // Block the operation when Session is missing.
            if (sessionReference == null)
            {
                tracingService.Trace(
                    "F8 blocked because Session is missing.");

                throw new InvalidPluginExecutionException(
                    "Please select a Session before submitting Feedback.");
            }

            tracingService.Trace(
                "Session reference received. Session ID: {0}",
                sessionReference.Id);

            // Retrieve only the Session Status column.
            Entity session = service.Retrieve(
                SessionTable,
                sessionReference.Id,
                new Microsoft.Xrm.Sdk.Query.ColumnSet(
                    SessionStatusColumn));

            tracingService.Trace(
                "Session record retrieved successfully.");

            // Read the Session Status choice value.
            OptionSetValue sessionStatus =
                session.GetAttributeValue<OptionSetValue>(
                    SessionStatusColumn);

            // Block when Session Status is blank.
            if (sessionStatus == null)
            {
                tracingService.Trace(
                    "F8 blocked because Session Status is blank.");

                throw new InvalidPluginExecutionException(
                    "Feedback cannot be submitted because the selected Session does not have a status.");
            }

            tracingService.Trace(
                "Session Status value received: {0}",
                sessionStatus.Value);

            // Allow Feedback only when Session Status is Completed.
            if (sessionStatus.Value != CompletedStatusValue)
            {
                tracingService.Trace(
                    "F8 blocked because Session Status is not Completed.");

                throw new InvalidPluginExecutionException(
                    "Feedback can be submitted only after the Session is Completed.");
            }

            tracingService.Trace(
                "F8 completed successfully. Session Status is Completed.");
        }
    }
}