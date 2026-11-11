using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Session
{
    /*
     * Requirement S6:
     * Prevent overlapping sessions
     * within the same Event.
     */
    public class SessionOverlapValidationPlugin : PluginBase
    {
        private const string SessionTable = "trpj_session";

        private const string EventColumn = "trpj_eventid";
        private const string StartTimeColumn = "trpj_start_time";
        private const string EndTimeColumn = "trpj_end_date";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "S6 Session overlap validation started.");

            if (!context.InputParameters.Contains("Target"))
            {
                return;
            }

            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                return;
            }

            Entity preImage = null;

            if (context.PreEntityImages.Contains("PreImage"))
            {
                preImage =
                    context.PreEntityImages["PreImage"];
            }

            EntityReference eventReference =
                target.Contains(EventColumn)
                ? target.GetAttributeValue<EntityReference>(
                    EventColumn)
                : preImage?.GetAttributeValue<EntityReference>(
                    EventColumn);

            DateTime? startTime =
                target.Contains(StartTimeColumn)
                ? target.GetAttributeValue<DateTime>(
                    StartTimeColumn)
                : preImage?.GetAttributeValue<DateTime>(
                    StartTimeColumn);

            DateTime? endTime =
                target.Contains(EndTimeColumn)
                ? target.GetAttributeValue<DateTime>(
                    EndTimeColumn)
                : preImage?.GetAttributeValue<DateTime>(
                    EndTimeColumn);

            if (eventReference == null ||
                startTime == null ||
                endTime == null)
            {
                tracingService.Trace(
                    "Required values missing.");

                return;
            }

            QueryExpression query =
                new QueryExpression(SessionTable);

            query.ColumnSet =
                new ColumnSet(false);

            query.Criteria.AddCondition(
                EventColumn,
                ConditionOperator.Equal,
                eventReference.Id);

            EntityCollection sessions =
                service.RetrieveMultiple(query);

            tracingService.Trace(
                "Found {0} session(s) under same Event.",
                sessions.Entities.Count);

            foreach (Entity session in sessions.Entities)
            {
                // Ignore current record
                if (session.Id == context.PrimaryEntityId)
                {
                    continue;
                }

                Entity existingSession =
                    service.Retrieve(
                        SessionTable,
                        session.Id,
                        new ColumnSet(
                            StartTimeColumn,
                            EndTimeColumn));

                DateTime existingStart =
                    existingSession.GetAttributeValue<DateTime>(
                        StartTimeColumn);

                DateTime existingEnd =
                    existingSession.GetAttributeValue<DateTime>(
                        EndTimeColumn);

                bool overlap =
                    startTime.Value < existingEnd &&
                    endTime.Value > existingStart;

                if (overlap)
                {
                    tracingService.Trace(
                        "Session overlap detected.");

                    throw new InvalidPluginExecutionException(
                        "Session timing overlaps with another Session in the selected Event.");
                }
            }

            tracingService.Trace(
                "S6 validation completed successfully.");
        }
    }
}
