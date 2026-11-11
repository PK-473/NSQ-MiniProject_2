using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Feedback
{
    /*
     * Requirement F5:
     * Only an Attendee whose Event Registration Status is Attended
     * can submit Feedback for a Session belonging to that Event.
     */
    public class ValidateFeedbackAttendancePlugin : PluginBase
    {
        // Feedback logical names
        private const string FeedbackTable = "trpj_feedback";
        private const string FeedbackAttendeeColumn = "trpj_attendee";
        private const string FeedbackSessionColumn = "trpj_session";

        // Session logical names
        private const string SessionTable = "trpj_session";
        private const string SessionEventColumn = "trpj_eventid";

        // Event Registration logical names
        private const string EventRegistrationTable =
            "trpj_eventregistration";

        private const string RegistrationEventColumn =
            "trpj_eventid";

        private const string RegistrationAttendeeColumn =
            "trpj_attendeeid";

        private const string RegistrationStatusColumn =
            "trpj_registrationstatus";

        // Registration Status choice value for Attended
        private const int AttendedStatusValue = 738000002;

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "F5 started. Message: {0}, Stage: {1}, Mode: {2}",
                context.MessageName,
                context.Stage,
                context.Mode);

            // F5 runs only when Feedback is created.
            if (!context.MessageName.Equals(
                    "Create",
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "F5 stopped because the message is not Create.");

                return;
            }

            // Confirm Target exists.
            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "F5 stopped because Target is missing.");

                return;
            }

            // Get the Feedback record being created.
            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "F5 stopped because Target is not an Entity.");

                return;
            }

            tracingService.Trace(
                "Target received. Logical name: {0}",
                target.LogicalName);

            // Confirm Target is Feedback.
            if (!target.LogicalName.Equals(
                    FeedbackTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "F5 stopped because Target is not Feedback.");

                return;
            }

            // Get the selected Attendee from Feedback.
            EntityReference attendeeReference =
                target.GetAttributeValue<EntityReference>(
                    FeedbackAttendeeColumn);

            // Get the selected Session from Feedback.
            EntityReference sessionReference =
                target.GetAttributeValue<EntityReference>(
                    FeedbackSessionColumn);

            if (attendeeReference == null)
            {
                tracingService.Trace(
                    "F5 blocked because Attendee is missing.");

                throw new InvalidPluginExecutionException(
                    "Please select an Attendee before submitting Feedback.");
            }

            if (sessionReference == null)
            {
                tracingService.Trace(
                    "F5 blocked because Session is missing.");

                throw new InvalidPluginExecutionException(
                    "Please select a Session before submitting Feedback.");
            }

            tracingService.Trace(
                "Attendee and Session references received.");

            /*
             * Retrieve the Event linked to the selected Session.
             * Only the Event lookup column is required.
             */
            Entity session = service.Retrieve(
                SessionTable,
                sessionReference.Id,
                new ColumnSet(SessionEventColumn));

            tracingService.Trace(
                "Session retrieved successfully.");

            EntityReference eventReference =
                session.GetAttributeValue<EntityReference>(
                    SessionEventColumn);

            if (eventReference == null)
            {
                tracingService.Trace(
                    "F5 blocked because the Session has no Event.");

                throw new InvalidPluginExecutionException(
                    "Feedback cannot be submitted because the selected Session is not linked to an Event.");
            }

            tracingService.Trace(
                "Session Event reference received. Event ID: {0}",
                eventReference.Id);

            /*
             * Search for a matching Event Registration where:
             * Event = Session's Event
             * AND Attendee = Feedback Attendee
             * AND Registration Status = Attended
             */
            QueryExpression query =
                new QueryExpression(EventRegistrationTable);

            // Only existence is required.
            query.ColumnSet = new ColumnSet(false);
            query.TopCount = 1;

            query.Criteria.AddCondition(
                RegistrationEventColumn,
                ConditionOperator.Equal,
                eventReference.Id);

            query.Criteria.AddCondition(
                RegistrationAttendeeColumn,
                ConditionOperator.Equal,
                attendeeReference.Id);

            query.Criteria.AddCondition(
                RegistrationStatusColumn,
                ConditionOperator.Equal,
                AttendedStatusValue);

            tracingService.Trace(
                "Checking for an Attended Event Registration.");

            EntityCollection attendedRegistrations =
                service.RetrieveMultiple(query);

            tracingService.Trace(
                "Attendance query returned {0} matching record(s).",
                attendedRegistrations.Entities.Count);

            // Block Feedback if no attended registration exists.
            if (attendedRegistrations.Entities.Count == 0)
            {
                tracingService.Trace(
                    "F5 blocked because no Attended registration was found.");

                throw new InvalidPluginExecutionException(
                    "Feedback can be submitted only by an Attendee who attended the Event.");
            }

            tracingService.Trace(
                "F5 completed successfully. Attended registration found.");
        }
    }
}