
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Event
{
    // E6: When an Event becomes Completed,
    // all registrations with Registered status are automatically changed to No Show.
    public class EventCompletedNoShowPlugin : PluginBase
    {
        // Logical name of the Event Registration table.
        private const string RegistrationTable = "trpj_eventregistration";

        // Logical name of the Event lookup column in Event Registration.
        // This column connects each registration to its parent Event.
        private const string RegistrationEventColumn = "trpj_eventid";

        // Logical name of the Registration Status choice column.
        private const string RegistrationStatusColumn = "trpj_registrationstatus";

        // Logical name of the Event Status choice column.
        private const string EventStatusColumn = "trpj_eventstatus";

        // Numeric value representing the Completed status of an Event.
        private const int StatusCompleted = 100000002;

        // Numeric value representing the Registered status of a registration.
        private const int StatusRegistered = 738000000;

        // Numeric value representing the No Show status of a registration.
        private const int StatusNoShow = 738000003;

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracing)
        {
            // Retrieve the Event record being created or updated
            // from the plugin execution context.
            var target = (Entity)context.InputParameters["Target"];

            // Retrieve the Event Status value from the Target record.
            // OptionSetValue is used for Dataverse Choice columns.
            var status = target.GetAttributeValue<OptionSetValue>(EventStatusColumn);

            // Stop execution if Event Status is not provided
            // or if the Event is not being changed to Completed.
            if (status == null || status.Value != StatusCompleted)
            {
                return;
            }

            // Create a query to retrieve related Event Registration records.
            // Only the Registration Status column is selected because
            // that is the only field required for this operation.
            var query = new QueryExpression(RegistrationTable)
            {
                ColumnSet = new ColumnSet(RegistrationStatusColumn)
            };

            // Retrieve registrations that belong to the current Event.
            query.Criteria.AddCondition(
                RegistrationEventColumn,
                ConditionOperator.Equal,
                target.Id);

            // Further filter the results to include only registrations
            // whose current status is Registered.
            query.Criteria.AddCondition(
                RegistrationStatusColumn,
                ConditionOperator.Equal,
                StatusRegistered);

            // Execute the query and loop through all matching registrations.
            foreach (var registration in service.RetrieveMultiple(query).Entities)
            {
                // Create a new Entity object containing the table name
                // and the ID of the registration that needs to be updated.
                var update = new Entity(RegistrationTable, registration.Id);

                // Change the Registration Status to No Show.
                update[RegistrationStatusColumn] = new OptionSetValue(StatusNoShow);

                // Save the updated Registration record in Dataverse.
                service.Update(update);

                // Write a trace message for tracking and debugging.
                // This records which registration was changed to No Show.
                tracing.Trace(
                    "Registration " + registration.Id + " set to No Show.");
            }
        }
    }
}
