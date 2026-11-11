using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;
using System;
using System.Linq;
using System.Security.Policy;

namespace NSQ.EventManagement.Plugins.Event
{
    public class EventCodeGeneratorPlugin : PluginBase
    {
        // Names used by this class (change here if a column is ever renamed).
        private const string EventTable = "trpj_event";
        private const string EventCodeColumn = "trpj_eventcode";
        private const string CodePrefix = "EN-";

        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];
            string prefix = CodePrefix + DateTime.UtcNow.ToString("ddMMyy") + "-";

            var query = new QueryExpression(EventTable)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet(EventCodeColumn)
            };
            query.Criteria.AddCondition(EventCodeColumn, ConditionOperator.BeginsWith, prefix);
            query.Orders.Add(new OrderExpression(EventCodeColumn, OrderType.Descending));

            var last = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            int next = 1;
            if (last != null)
            {
                string lastCode = last.GetAttributeValue<string>(EventCodeColumn);
                next = int.Parse(lastCode.Substring(prefix.Length)) + 1;
            }
            target[EventCodeColumn] = prefix + next.ToString("000");
        }
    }
}
