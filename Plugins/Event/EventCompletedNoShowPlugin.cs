using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Event
{
    // E6: when an event becomes Completed, registrations still Registered become No Show.
    public class EventCompletedNoShowPlugin : PluginBase
    {
        private const string RegistrationTable = "trpj_eventregistration";
        private const string RegistrationEventColumn = "trpj_eventid";
        private const string RegistrationStatusColumn = "trpj_registrationstatus";
        private const string EventStatusColumn = "trpj_eventstatus";
        private const int StatusCompleted = 100000002;
        private const int StatusRegistered = 100000000;
        private const int StatusNoShow = 100000003;

        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];

            var status = target.GetAttributeValue<OptionSetValue>(EventStatusColumn);
            if (status == null || status.Value != StatusCompleted) { return; }

            var query = new QueryExpression(RegistrationTable) { ColumnSet = new ColumnSet(RegistrationStatusColumn) };
            query.Criteria.AddCondition(RegistrationEventColumn, ConditionOperator.Equal, target.Id);
            query.Criteria.AddCondition(RegistrationStatusColumn, ConditionOperator.Equal, StatusRegistered);

            foreach (var registration in service.RetrieveMultiple(query).Entities)
            {
                var update = new Entity(RegistrationTable, registration.Id);
                update[RegistrationStatusColumn] = new OptionSetValue(StatusNoShow);
                service.Update(update);
                tracing.Trace("Registration " + registration.Id + " set to No Show.");
            }
        }
    }
}