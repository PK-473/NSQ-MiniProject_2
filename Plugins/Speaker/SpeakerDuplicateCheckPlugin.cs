using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Speaker
{
    /*
     * Requirements:
     *
     * SP1:
     * Prevent duplicate Email Address and Phone Number.
     *
     * SP5:
     * LinkedIn Profile must be unique.
     */
    public class SpeakerDuplicateCheckPlugin : PluginBase
    {
        // Table Name
        private const string SpeakerTable = "trpj_speaker";

        // Column Names
        private const string EmailColumn = "emailaddress";
        private const string PhoneColumn = "trpj_phone_number";
        private const string LinkedInColumn = "trpj_linkedin_profile";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "Speaker duplicate validation started. Message: {0}",
                context.MessageName);

            // Validate Target
            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "Target input parameter not found.");

                return;
            }

            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "Target is not an Entity.");

                return;
            }

            tracingService.Trace(
                "Target Logical Name: {0}",
                target.LogicalName);

            if (!target.LogicalName.Equals(
                    SpeakerTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "Entity is not Speaker.");

                return;
            }

            Guid currentRecordId =
                context.PrimaryEntityId;

            /*
             * Email Validation
             */
            if (target.Contains(EmailColumn))
            {
                string email =
                    target.GetAttributeValue<string>(
                        EmailColumn);

                if (!string.IsNullOrWhiteSpace(email))
                {
                    tracingService.Trace(
                        "Checking duplicate Email: {0}",
                        email);

                    ValidateDuplicate(
                        service,
                        currentRecordId,
                        EmailColumn,
                        email.Trim().ToLower(),
                        "A Speaker with the same Email Address already exists.",
                        tracingService);
                }
            }

            /*
             * Phone Validation
             */
            if (target.Contains(PhoneColumn))
            {
                string phone =
                    target.GetAttributeValue<string>(
                        PhoneColumn);

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    tracingService.Trace(
                        "Checking duplicate Phone Number: {0}",
                        phone);

                    ValidateDuplicate(
                        service,
                        currentRecordId,
                        PhoneColumn,
                        phone.Trim(),
                        "A Speaker with the same Phone Number already exists.",
                        tracingService);
                }
            }

            /*
             * LinkedIn Validation
             */
            if (target.Contains(LinkedInColumn))
            {
                string linkedIn =
                    target.GetAttributeValue<string>(
                        LinkedInColumn);

                if (!string.IsNullOrWhiteSpace(linkedIn))
                {
                    tracingService.Trace(
                        "Checking duplicate LinkedIn Profile.");

                    ValidateDuplicate(
                        service,
                        currentRecordId,
                        LinkedInColumn,
                        linkedIn.Trim(),
                        "A Speaker with the same LinkedIn Profile already exists.",
                        tracingService);
                }
            }

            tracingService.Trace(
                "Speaker duplicate validation completed successfully.");
        }

        private void ValidateDuplicate(
            IOrganizationService service,
            Guid currentRecordId,
            string columnName,
            string value,
            string errorMessage,
            ITracingService tracingService)
        {
            QueryExpression query =
                new QueryExpression(SpeakerTable);

            query.ColumnSet =
                new ColumnSet(false);

            query.Criteria.AddCondition(
                columnName,
                ConditionOperator.Equal,
                value);

            EntityCollection records =
                service.RetrieveMultiple(query);

            tracingService.Trace(
                "Duplicate query returned {0} record(s).",
                records.Entities.Count);

            foreach (Entity record in records.Entities)
            {
                if (record.Id != currentRecordId)
                {
                    tracingService.Trace(
                        "Duplicate record found.");

                    throw new InvalidPluginExecutionException(
                        errorMessage);
                }
            }
        }
    }
}