using System;
using Microsoft.Xrm.Sdk;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.Feedback
{
    /*
     * Requirement F2:
     * Automatically populate Submitted On with the current date and time
     * when a new Feedback record is created.
     */
    public class SetFeedbackSubmittedOnPlugin : PluginBase
    {
        // Logical names used only by this plug-in
        private const string FeedbackTable = "trpj_feedback";
        private const string SubmittedOnColumn = "trpj_submittedon";

        protected override void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            // Run the logic only during Feedback creation
            if (!context.MessageName.Equals(
                    "Create",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Check whether Target exists and contains a Dataverse record
            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity target))
            {
                return;
            }

            // Ensure the record is from the Feedback table
            if (target.LogicalName != FeedbackTable)
            {
                return;
            }

            // Set the server's current UTC date and time before saving
            target[SubmittedOnColumn] = DateTime.UtcNow;

            tracingService.Trace(
                "F2: Submitted On was populated successfully.");
        }
    }
}
