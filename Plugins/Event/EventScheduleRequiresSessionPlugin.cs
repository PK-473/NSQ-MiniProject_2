using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Event
{
    // E9: an event cannot move to Scheduled unless at least one session exists.
    public class EventScheduleRequiresSessionPlugin : PluginBase
    {
        private const string SessionTable = "trpj_session";
        private const string SessionEventColumn = "trpj_eventid";
        private const string EventStatusColumn = "trpj_eventstatus";
        private const int StatusScheduled = 100000001;

        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];

            var status = target.GetAttributeValue<OptionSetValue>(EventStatusColumn);
            if (status == null || status.Value != StatusScheduled) { return; }

            var query = new QueryExpression(SessionTable) { TopCount = 1, ColumnSet = new ColumnSet(false) };
            query.Criteria.AddCondition(SessionEventColumn, ConditionOperator.Equal, target.Id);

            if (service.RetrieveMultiple(query).Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException("An event cannot be Scheduled until at least one session has been added.");
            }
        }
    }
}
