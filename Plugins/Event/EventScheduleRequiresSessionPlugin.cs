
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Event
{
    // E9: Prevents an Event from moving to Scheduled status
    // unless at least one Session has been created for that Event.
    public class EventScheduleRequiresSessionPlugin : PluginBase
    {
        // Logical name of the Session table.
        private const string SessionTable = "trpj_session";

        // Logical name of the lookup column connecting Session to Event.
        private const string SessionEventColumn = "trpj_eventid";

        // Logical name of the Event Status column.
        private const string EventStatusColumn = "trpj_eventstatus";

        // Choice value representing the Scheduled Event status.
        private const int StatusScheduled = 100000001;

        // Main method executed when the registered plugin step runs.
        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracing)
        {
            // Retrieve the Event record being created or updated.
            // Target contains the columns submitted in the current operation.
            var target = (Entity)context.InputParameters["Target"];

            // Get the Event Status value from Target.
            // OptionSetValue is used for Dataverse Choice columns.
            var status = target.GetAttributeValue<OptionSetValue>(
                EventStatusColumn);

            // Continue only if the Event Status is Scheduled.
            // If the status is missing or has another value,
            // no further validation is required.
            if (status == null || status.Value != StatusScheduled)
            {
                return;
            }

            // Create a query to check whether at least one Session
            // is associated with the current Event.
            var query = new QueryExpression(SessionTable)
            {
                // Retrieve only one matching record because we only
                // need to know whether a Session exists.
                TopCount = 1,

                // No columns are required because we only check existence.
                // This reduces the amount of data retrieved.
                ColumnSet = new ColumnSet(false)
            };

            // Filter Sessions using the Event lookup column.
            // Only Sessions associated with the current Event are considered.
            query.Criteria.AddCondition(
                SessionEventColumn,
                ConditionOperator.Equal,
                target.Id);

            // Execute the query to retrieve matching Sessions.
            // If the result contains zero records, the Event has no Sessions.
            if (service.RetrieveMultiple(query).Entities.Count == 0)
            {
                // Throw an exception to prevent the Event from being
                // moved to Scheduled status.
                // Dataverse displays this message to the user.
                throw new InvalidPluginExecutionException(
                    "An event cannot be Scheduled until at least one session has been added.");
            }
        }
    }
}
