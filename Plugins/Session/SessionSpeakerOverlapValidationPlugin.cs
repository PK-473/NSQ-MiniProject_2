using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Session
{
    /*
     * Requirement S8:
     * Same Speaker cannot be assigned
     * to overlapping Sessions.
     */
    public class SpeakerSpeakerOverlapValidationPlugin : PluginBase
    {
        private const string SessionTable = "trpj_session";

        private const string SpeakerColumn = "trpj_speakerid";
        private const string StartTimeColumn = "trpj_start_time";
        private const string EndTimeColumn = "trpj_end_date";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "S8 Speaker overlap validation started.");

            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "Target not found.");

                return;
            }

            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "Target is null.");

                return;
            }

            Entity preImage = null;

            if (context.PreEntityImages.Contains("PreImage"))
            {
                preImage =
                    context.PreEntityImages["PreImage"];
            }

            EntityReference speaker =
                target.Contains(SpeakerColumn)
                ? target.GetAttributeValue<EntityReference>(
                    SpeakerColumn)
                : preImage?.GetAttributeValue<EntityReference>(
                    SpeakerColumn);

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

            if (speaker == null ||
                startTime == null ||
                endTime == null)
            {
                tracingService.Trace(
                    "Speaker, Start Time or End Time missing.");

                return;
            }

            tracingService.Trace(
                "Checking overlapping sessions for Speaker: {0}",
                speaker.Id);

            QueryExpression query =
                new QueryExpression(SessionTable);

            query.ColumnSet =
                new ColumnSet(
                    SpeakerColumn,
                    StartTimeColumn,
                    EndTimeColumn);

            query.Criteria.AddCondition(
                SpeakerColumn,
                ConditionOperator.Equal,
                speaker.Id);

            EntityCollection sessions =
                service.RetrieveMultiple(query);

            tracingService.Trace(
                "Found {0} Sessions for Speaker.",
                sessions.Entities.Count);

            foreach (Entity session in sessions.Entities)
            {
                // Skip current session
                if (session.Id == context.PrimaryEntityId)
                {
                    continue;
                }

                DateTime existingStart =
                    session.GetAttributeValue<DateTime>(
                        StartTimeColumn);

                DateTime existingEnd =
                    session.GetAttributeValue<DateTime>(
                        EndTimeColumn);

                bool overlap =
                    startTime.Value < existingEnd &&
                    endTime.Value > existingStart;

                if (overlap)
                {
                    tracingService.Trace(
                        "Speaker overlap detected.");

                    throw new InvalidPluginExecutionException(
                        "This Speaker is already assigned to another Session during the selected time period.");
                }
            }

            tracingService.Trace(
                "S8 validation completed successfully.");
        }
    }
}