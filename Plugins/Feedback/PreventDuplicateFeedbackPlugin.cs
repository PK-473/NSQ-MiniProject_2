using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Feedback
{
    /*
     * Requirement F10:
     * Prevent the same Attendee from submitting more than one
     * Feedback record for the same Session.
     */
    public class PreventDuplicateFeedbackPlugin : PluginBase
    {
        // Logical names used by this plug-in
        private const string FeedbackTable = "trpj_feedback";
        private const string AttendeeColumn = "trpj_attendee";
        private const string SessionColumn = "trpj_session";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "F10 started. Message: {0}, Stage: {1}, Mode: {2}",
                context.MessageName,
                context.Stage,
                context.Mode);

            // F10 runs only when a new Feedback record is created.
            if (!context.MessageName.Equals(
                    "Create",
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "F10 stopped because the message is not Create.");

                return;
            }

            // Confirm that the Target input parameter exists.
            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "F10 stopped because Target is missing.");

                return;
            }

            // Get the Feedback record being created.
            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "F10 stopped because Target is not an Entity.");

                return;
            }

            tracingService.Trace(
                "Target received. Logical name: {0}",
                target.LogicalName);

            // Confirm the Target is a Feedback record.
            if (!target.LogicalName.Equals(
                    FeedbackTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "F10 stopped because Target is not Feedback.");

                return;
            }

            // Get the selected Attendee lookup.
            EntityReference attendeeReference =
                target.GetAttributeValue<EntityReference>(
                    AttendeeColumn);

            // Get the selected Session lookup.
            EntityReference sessionReference =
                target.GetAttributeValue<EntityReference>(
                    SessionColumn);

            // Block safely when Attendee is missing.
            if (attendeeReference == null)
            {
                tracingService.Trace(
                    "F10 blocked because Attendee is missing.");

                throw new InvalidPluginExecutionException(
                    "Please select an Attendee before submitting Feedback.");
            }

            // Block safely when Session is missing.
            if (sessionReference == null)
            {
                tracingService.Trace(
                    "F10 blocked because Session is missing.");

                throw new InvalidPluginExecutionException(
                    "Please select a Session before submitting Feedback.");
            }

            tracingService.Trace(
                "Attendee and Session references received.");

            /*
             * Search Feedback for an existing record having:
             * Attendee = selected Attendee
             * AND
             * Session = selected Session
             */
            QueryExpression query =
                new QueryExpression(FeedbackTable);

            // We only need to know whether a record exists.
            query.ColumnSet = new ColumnSet(false);

            // Retrieve a maximum of one matching record.
            query.TopCount = 1;

            query.Criteria.AddCondition(
                AttendeeColumn,
                ConditionOperator.Equal,
                attendeeReference.Id);

            query.Criteria.AddCondition(
                SessionColumn,
                ConditionOperator.Equal,
                sessionReference.Id);

            tracingService.Trace(
                "Checking for existing Feedback with the same Attendee and Session.");

            EntityCollection existingFeedback =
                service.RetrieveMultiple(query);

            tracingService.Trace(
                "Duplicate query returned {0} matching record(s).",
                existingFeedback.Entities.Count);

            // Block creation when a matching Feedback already exists.
            if (existingFeedback.Entities.Count > 0)
            {
                tracingService.Trace(
                    "F10 blocked duplicate Feedback creation.");

                throw new InvalidPluginExecutionException(
                    "Feedback has already been submitted by this Attendee for the selected Session.");
            }

            tracingService.Trace(
                "F10 completed successfully. No duplicate Feedback was found.");
        }
    }
}