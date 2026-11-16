using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Session
{
    /*
     * Prevent deletion of a Session when one or more
     * related Feedback records exist.
     */
    public class PreventSessionDeletionWithFeedbackPlugin : PluginBase
    {
        private const string SessionTable = "trpj_session";
        private const string FeedbackTable = "trpj_feedback";
        private const string FeedbackSessionColumn = "trpj_session";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "PreventSessionDeletionWithFeedbackPlugin started. Message: {0}, Stage: {1}, Mode: {2}",
                context.MessageName,
                context.Stage,
                context.Mode);

            // Run only when a Session is deleted.
            if (!context.MessageName.Equals(
                    "Delete",
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "Plugin stopped because the message is not Delete.");

                return;
            }

            // Delete provides Target as an EntityReference.
            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "Plugin stopped because Target is missing.");

                return;
            }

            EntityReference sessionReference =
                context.InputParameters["Target"] as EntityReference;

            if (sessionReference == null)
            {
                tracingService.Trace(
                    "Plugin stopped because Target is not an EntityReference.");

                return;
            }

            tracingService.Trace(
                "Delete Target received. Logical name: {0}, Session ID: {1}",
                sessionReference.LogicalName,
                sessionReference.Id);

            // Confirm the deleted record belongs to the Session table.
            if (!sessionReference.LogicalName.Equals(
                    SessionTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "Plugin stopped because Target is not a Session.");

                return;
            }

            // Search for one Feedback record related to this Session.
            QueryExpression query = new QueryExpression(FeedbackTable)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1
            };

            query.Criteria.AddCondition(
                FeedbackSessionColumn,
                ConditionOperator.Equal,
                sessionReference.Id);

            tracingService.Trace(
                "Checking for Feedback records related to the Session.");

            EntityCollection relatedFeedback =
                service.RetrieveMultiple(query);

            tracingService.Trace(
                "Related Feedback query returned {0} record(s).",
                relatedFeedback.Entities.Count);

            // Block deletion when related Feedback exists.
            if (relatedFeedback.Entities.Count > 0)
            {
                tracingService.Trace(
                    "Session deletion blocked because related Feedback exists.");

                throw new InvalidPluginExecutionException(
                    "This Session cannot be deleted because related Feedback records exist. Delete the related Feedback records first.");
            }

            tracingService.Trace(
                "Plugin completed successfully. No related Feedback exists.");
        }
    }
}