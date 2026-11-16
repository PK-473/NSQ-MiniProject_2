using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;
using System;
using System.Security.Policy;

namespace NSQ.EventManagement.Plugins.Event
{
    // E4: Prevents two events from being scheduled at the same location
    // during overlapping date and time ranges.
    public class EventOverlapValidationPlugin : PluginBase
    {
        // Logical name of the Event table in Dataverse.
        private const string EventTable = "trpj_event";

        // Logical names of the Event table columns.
        private const string EventIdColumn = "trpj_eventid";
        private const string NameColumn = "trpj_eventname";
        private const string StartColumn = "trpj_startdate";
        private const string EndColumn = "trpj_enddate";
        private const string LocationColumn = "trpj_location";
        private const string StatusColumn = "trpj_eventstatus";

        // Choice value representing a Cancelled Event.
        // Cancelled events are excluded from overlap validation.
        private const int StatusCancelled = 100000003;

        // Main method executed when the registered plugin step runs.
        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracing)
        {
            // Retrieve the record being created or updated.
            // Target contains the columns submitted in the current operation.
            var target = (Entity)context.InputParameters["Target"];

            // On Update, Target normally contains only the modified columns.
            // Therefore, retrieve the existing Event record to get values
            // for columns that were not included in Target.
            Entity current = null;

            if (context.MessageName == "Update")
            {
                current = service.Retrieve(
                    EventTable,
                    target.Id,
                    new ColumnSet(StartColumn, EndColumn, LocationColumn));
            }

            // Read the Start Date from Target if supplied;
            // otherwise, read it from the existing saved record.
            DateTime? start = ReadValue<DateTime?>(
                target, current, StartColumn);

            // Read the End Date using the same approach.
            DateTime? end = ReadValue<DateTime?>(
                target, current, EndColumn);

            // Read the Location from Target or the saved record.
            string location = ReadValue<string>(
                target, current, LocationColumn);

            // Stop validation if Start Date, End Date, or Location
            // is missing or empty.
            if (start == null ||
                end == null ||
                string.IsNullOrWhiteSpace(location))
            {
                return;
            }

            // Find existing Events whose date ranges overlap
            // with the Event currently being created or updated.
            //
            // Overlap condition:
            // Existing Start Date < New End Date
            // AND
            // Existing End Date > New Start Date
            //
            // This also allows one event to end exactly when
            // another event starts.

            var query = new QueryExpression(EventTable)
            {
                // Retrieve only the columns needed for validation.
                ColumnSet = new ColumnSet(NameColumn, LocationColumn)
            };

            // Existing Event must start before the new Event ends.
            query.Criteria.AddCondition(
                StartColumn,
                ConditionOperator.LessThan,
                end.Value);

            // Existing Event must end after the new Event starts.
            query.Criteria.AddCondition(
                EndColumn,
                ConditionOperator.GreaterThan,
                start.Value);

            // Exclude the current Event from the search.
            // This is important during Update because the current
            // record must not be compared against itself.
            query.Criteria.AddCondition(
                EventIdColumn,
                ConditionOperator.NotEqual,
                target.Id);

            // Ignore Events whose status is Cancelled.
            query.Criteria.AddCondition(
                StatusColumn,
                ConditionOperator.NotEqual,
                StatusCancelled);

            // Remove leading and trailing spaces from the new Location.
            string wanted = location.Trim();

            // Retrieve all Events matching the date overlap conditions.
            foreach (var other in service.RetrieveMultiple(query).Entities)
            {
                // Get the Location of the existing Event.
                // Location is compared in C# because it is a multi-line
                // text column and the comparison needs trimming
                // and case-insensitive matching.
                string otherLocation =
                    other.GetAttributeValue<string>(LocationColumn);

                // Check whether the existing Event has a valid Location
                // and whether both Locations match.
                //
                // StringComparison.OrdinalIgnoreCase means that
                // uppercase and lowercase differences are ignored.
                //
                // Example:
                // "Pune Hall" and "pune hall" are treated as equal.
                if (!string.IsNullOrWhiteSpace(otherLocation) &&
                    string.Equals(
                        otherLocation.Trim(),
                        wanted,
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Stop the current Dataverse operation and display
                    // an error message to the user.
                    // The conflicting Event's name is included in the message.
                    throw new InvalidPluginExecutionException(
                        "Another event (" +
                        other.GetAttributeValue<string>(NameColumn) +
                        ") is already scheduled at this location during this time.");
                }
            }
        }

        // Helper method to retrieve a column value.
        //
        // First, check whether the column is present in Target.
        // If yes, use the new value submitted by the user.
        //
        // Otherwise, use the value from the existing saved record.
        //
        // If neither record is available, return the default value.
        private static T ReadValue<T>(
            Entity target,
            Entity saved,
            string column)
        {
            if (target.Contains(column))
            {
                return target.GetAttributeValue<T>(column);
            }

            return saved != null
                ? saved.GetAttributeValue<T>(column)
                : default(T);
        }
    }
}
