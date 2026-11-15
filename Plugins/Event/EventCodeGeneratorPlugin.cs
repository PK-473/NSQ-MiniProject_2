using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;
using System;
using System.Linq;
using System.Security.Policy;

namespace NSQ.EventManagement.Plugins.Event
{
    // Plugin to automatically generate a unique Event Code
    // whenever a new Event record is created.
    public class EventCodeGeneratorPlugin : PluginBase
    {
        // Logical name of the Event table in Dataverse.
        private const string EventTable = "trpj_event";

        // Logical name of the Event Code column.
        private const string EventCodeColumn = "trpj_eventcode";

        // Fixed prefix used for every generated Event Code.
        private const string CodePrefix = "EN-";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracing)
        {
            // Retrieve the Event record being created from the plugin context.
            var target = (Entity)context.InputParameters["Target"];

            // Generate the date-based prefix using the current UTC date.
            // Example: EN-151126- for 15 November 2026.
            string prefix = CodePrefix + DateTime.UtcNow.ToString("ddMMyy") + "-";

            // Create a query to find the latest Event Code generated for today.
            var query = new QueryExpression(EventTable)
            {
                // Retrieve only one matching Event record.
                TopCount = 1,

                // Retrieve only the Event Code column.
                ColumnSet = new ColumnSet(EventCodeColumn)
            };

            // Filter records to find Event Codes starting with today's prefix.
            query.Criteria.AddCondition(
                EventCodeColumn,
                ConditionOperator.BeginsWith,
                prefix);

            // Sort Event Codes in descending order so the highest code is retrieved first.
            query.Orders.Add(
                new OrderExpression(EventCodeColumn, OrderType.Descending));

            // Execute the query and retrieve the first matching Event record.
            // FirstOrDefault() returns null if no matching record exists.
            var last = service.RetrieveMultiple(query).Entities.FirstOrDefault();

            // Start the sequence number from 1.
            int next = 1;

            // Check whether an Event Code has already been generated today.
            if (last != null)
            {
                // Retrieve the Event Code of the latest matching Event.
                string lastCode = last.GetAttributeValue<string>(EventCodeColumn);

                // Extract the numeric portion of the Event Code,
                // convert it to an integer and increment it by 1.
                next = int.Parse(lastCode.Substring(prefix.Length)) + 1;
            }

            // Assign the generated Event Code to the Event record.
            // ToString("000") formats the number with three digits.
            // Example: EN-151126-001, EN-151126-002, EN-151126-003.
            target[EventCodeColumn] = prefix + next.ToString("000");
        }
    }
}
