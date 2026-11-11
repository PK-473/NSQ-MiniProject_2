using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Event
{
    // E4: no two events at the same location may overlap in time.
    public class EventOverlapValidationPlugin : PluginBase
    {
        private const string EventTable = "trpj_event";
        private const string EventIdColumn = "trpj_eventid";
        private const string NameColumn = "trpj_eventname";
        private const string StartColumn = "trpj_startdate";
        private const string EndColumn = "trpj_enddate";
        private const string LocationColumn = "trpj_location";
        private const string StatusColumn = "trpj_eventstatus";
        private const int StatusCancelled = 100000003;

        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];

            // On Update, Target holds only the changed columns, so read the rest from the saved record.
            Entity current = null;
            if (context.MessageName == "Update")
            {
                current = service.Retrieve(EventTable, target.Id, new ColumnSet(StartColumn, EndColumn, LocationColumn));
            }

            DateTime? start = ReadValue<DateTime?>(target, current, StartColumn);
            DateTime? end = ReadValue<DateTime?>(target, current, EndColumn);
            string location = ReadValue<string>(target, current, LocationColumn);

            if (start == null || end == null || string.IsNullOrWhiteSpace(location)) { return; }

            // Two ranges overlap when existing.start < new.end AND existing.end > new.start.
            var query = new QueryExpression(EventTable) { ColumnSet = new ColumnSet(NameColumn, LocationColumn) };
            query.Criteria.AddCondition(StartColumn, ConditionOperator.LessThan, end.Value);
            query.Criteria.AddCondition(EndColumn, ConditionOperator.GreaterThan, start.Value);
            query.Criteria.AddCondition(EventIdColumn, ConditionOperator.NotEqual, target.Id);
            query.Criteria.AddCondition(StatusColumn, ConditionOperator.NotEqual, StatusCancelled);

            string wanted = location.Trim();
            foreach (var other in service.RetrieveMultiple(query).Entities)
            {
                // Location is a multi-line column, so it is compared here instead of in the query.
                string otherLocation = other.GetAttributeValue<string>(LocationColumn);
                if (!string.IsNullOrWhiteSpace(otherLocation) &&
                    string.Equals(otherLocation.Trim(), wanted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidPluginExecutionException(
                        "Another event (" + other.GetAttributeValue<string>(NameColumn) +
                        ") is already scheduled at this location during this time.");
                }
            }
        }
        // New value if the column is in the Target; otherwise the value from the saved record.
        private static T ReadValue<T>(Entity target, Entity saved, string column)
        {
            if (target.Contains(column)) { return target.GetAttributeValue<T>(column); }
            return saved != null ? saved.GetAttributeValue<T>(column) : default(T);
        }
    }
}