using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Feedback
{
    /*
     * Prevent creating or updating Feedback when Comments
     * contains a forbidden whole word.
     *
     * Forbidden words are read from the environment variable:
     * trpj_ForbiddenWords
     */
    public class FeedbackProfanityValidationPlugin : PluginBase
    {
        // Feedback logical names
        private const string FeedbackTable = "trpj_feedback";
        private const string CommentsColumn = "trpj_comments";

        // Environment variable schema name
        private const string ForbiddenWordsVariable =
            "trpj_ForbiddenWords";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "FeedbackProfanityValidationPlugin started.");

            // Run only on Create and Update
            if (!context.MessageName.Equals(
                    "Create",
                    StringComparison.OrdinalIgnoreCase) &&
                !context.MessageName.Equals(
                    "Update",
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "Plugin stopped because the message is not Create or Update.");

                return;
            }

            // Check whether Target exists
            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "Plugin stopped because Target is missing.");

                return;
            }

            // Get the Feedback record
            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "Plugin stopped because Target is not an Entity.");

                return;
            }

            // Confirm that Target is Feedback
            if (!target.LogicalName.Equals(
                    FeedbackTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "Plugin stopped because Target is not Feedback.");

                return;
            }

            /*
             * On Update, Target contains only changed columns.
             * If Comments is not changed, validation is not needed.
             */
            if (!target.Contains(CommentsColumn))
            {
                tracingService.Trace(
                    "Comments is not present in Target.");

                return;
            }

            // Get Comments
            string comments =
                target.GetAttributeValue<string>(CommentsColumn);

            // Allow blank Comments
            if (string.IsNullOrWhiteSpace(comments))
            {
                tracingService.Trace(
                    "Comments is blank.");

                return;
            }

            /*
             * STEP 1:
             * Find the environment variable definition using
             * schema name trpj_ForbiddenWords.
             */
            QueryExpression definitionQuery =
                new QueryExpression(
                    "environmentvariabledefinition");

            definitionQuery.ColumnSet =
                new ColumnSet(
                    "environmentvariabledefinitionid",
                    "defaultvalue");

            definitionQuery.TopCount = 1;

            definitionQuery.Criteria.AddCondition(
                "schemaname",
                ConditionOperator.Equal,
                ForbiddenWordsVariable);

            EntityCollection definitions =
                service.RetrieveMultiple(definitionQuery);

            if (definitions.Entities.Count == 0)
            {
                tracingService.Trace(
                    "Forbidden Words environment variable was not found.");

                throw new InvalidPluginExecutionException(
                    "Forbidden-words validation is not configured. Please contact the administrator.");
            }

            Entity definition =
                definitions.Entities[0];

            Guid definitionId =
                definition.Id;

            string defaultValue =
                definition.GetAttributeValue<string>(
                    "defaultvalue");

            tracingService.Trace(
                "Forbidden Words environment variable definition found.");

            /*
             * STEP 2:
             * Get the Current Value connected to the definition.
             */
            QueryExpression valueQuery =
                new QueryExpression(
                    "environmentvariablevalue");

            valueQuery.ColumnSet =
                new ColumnSet("value");

            valueQuery.TopCount = 1;

            valueQuery.Criteria.AddCondition(
                "environmentvariabledefinitionid",
                ConditionOperator.Equal,
                definitionId);

            EntityCollection currentValues =
                service.RetrieveMultiple(valueQuery);

            string currentValue = null;

            if (currentValues.Entities.Count > 0)
            {
                currentValue =
                    currentValues.Entities[0]
                        .GetAttributeValue<string>("value");
            }

            /*
             * Use Current Value when available.
             * Otherwise, use Default Value.
             */
            string forbiddenWordsText =
                !string.IsNullOrWhiteSpace(currentValue)
                    ? currentValue
                    : defaultValue;

            if (string.IsNullOrWhiteSpace(forbiddenWordsText))
            {
                tracingService.Trace(
                    "Forbidden Words environment variable is empty.");

                throw new InvalidPluginExecutionException(
                    "Forbidden-words validation is not configured. Please contact the administrator.");
            }

            /*
             * Convert the comma-separated environment variable
             * value into a string array.
             *
             * Example:
             * idiot,stupid,damn
             *
             * Becomes:
             * ["idiot", "stupid", "damn"]
             */
            string[] forbiddenWords = forbiddenWordsText
                .Split(
                    new[] { ',', ';', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Trim())
                .Where(word =>
                    !string.IsNullOrWhiteSpace(word))
                .ToArray();

            tracingService.Trace(
                "Forbidden Words loaded. Count: {0}",
                forbiddenWords.Length);

            // Check Comments against each configured word
            foreach (string forbiddenWord in forbiddenWords)
            {
                /*
                 * \b checks whole words only.
                 * IgnoreCase allows uppercase and lowercase variations.
                 */
                string pattern =
                    @"\b" +
                    Regex.Escape(forbiddenWord) +
                    @"\b";

                bool containsForbiddenWord =
                    Regex.IsMatch(
                        comments,
                        pattern,
                        RegexOptions.IgnoreCase |
                        RegexOptions.CultureInvariant);

                if (containsForbiddenWord)
                {
                    tracingService.Trace(
                        "Feedback blocked because a forbidden word was found.");

                    throw new InvalidPluginExecutionException(
                        "Comments contains inappropriate language. Remove the forbidden word and try again.");
                }
            }

            tracingService.Trace(
                "Plugin completed. No forbidden words were found.");
        }
    }
}