using System;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Feedback
{
    /*
     * Requirement F4:
     * Prevent creating or updating Feedback when Comments contains
     * a forbidden word.
     *
     * Matching rules:
     * 1. Check complete words only.
     * 2. Ignore uppercase and lowercase differences.
     * 3. Allow blank Comments.
     */
    public class FeedbackProfanityValidationPlugin : PluginBase
    {
        // Feedback table and column logical names
        private const string FeedbackTable = "trpj_feedback";
        private const string CommentsColumn = "trpj_comments";

        // Forbidden words approved for the simple profanity check
        private readonly string[] forbiddenWords =
 {
    "idiot", "stupid", "damn","nonsense","useless","fuck","shit","bitch","asshole","bastard","crap","dick"
};


        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            tracingService.Trace(
                "F4 started. Message: {0}, Stage: {1}, Mode: {2}",
                context.MessageName,
                context.Stage,
                context.Mode);

            // Run the validation only for Create and Update operations
            bool isCreate = context.MessageName.Equals(
                "Create",
                StringComparison.OrdinalIgnoreCase);

            bool isUpdate = context.MessageName.Equals(
                "Update",
                StringComparison.OrdinalIgnoreCase);

            if (!isCreate && !isUpdate)
            {
                tracingService.Trace(
                    "F4 stopped because the message is not Create or Update.");

                return;
            }

            // Confirm that the Target parameter exists
            if (!context.InputParameters.Contains("Target"))
            {
                tracingService.Trace(
                    "F4 stopped because Target is missing.");

                return;
            }

            // Get the Feedback record being created or updated
            Entity target =
                context.InputParameters["Target"] as Entity;

            if (target == null)
            {
                tracingService.Trace(
                    "F4 stopped because Target is not an Entity.");

                return;
            }

            tracingService.Trace(
                "Target received. Logical name: {0}",
                target.LogicalName);

            // Confirm that the Target belongs to the Feedback table
            if (!target.LogicalName.Equals(
                    FeedbackTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace(
                    "F4 stopped because Target is not Feedback.");

                return;
            }

            /*
             * During Update, Target contains only changed columns.
             * If Comments is not present, the user did not change Comments,
             * so no profanity validation is required.
             */
            if (!target.Contains(CommentsColumn))
            {
                tracingService.Trace(
                    "F4 completed because Comments is not present in Target.");

                return;
            }

            // Read the Comments value
            string comments =
                target.GetAttributeValue<string>(CommentsColumn);

            // Blank Comments is allowed
            if (string.IsNullOrWhiteSpace(comments))
            {
                tracingService.Trace(
                    "F4 completed because Comments is blank.");

                return;
            }

            tracingService.Trace(
                "Comments received. Starting whole-word validation.");

            // Check Comments against every forbidden word
            foreach (string forbiddenWord in forbiddenWords)
            {
                /*
                 * Regex.Escape safely handles any special character
                 * that might be present in a forbidden word.
                 *
                 * \b represents a word boundary, so only complete
                 * words are matched.
                 */
                string pattern =
                    @"\b" + Regex.Escape(forbiddenWord) + @"\b";

                bool containsForbiddenWord = Regex.IsMatch(
                    comments,
                    pattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant);

                if (containsForbiddenWord)
                {
                    tracingService.Trace(
                        "F4 blocked the operation because a forbidden word was found.");

                    /*
                     * Do not trace the complete Comments value because it
                     * may contain user-entered or inappropriate content.
                     */
                    throw new InvalidPluginExecutionException(
                        "Comments contains inappropriate language. Remove the forbidden word and try again.");
                }
            }

            tracingService.Trace(
                "F4 completed successfully. No forbidden words were found.");
        }
    }
}