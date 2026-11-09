using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    /// <summary>
    /// R10 | Event Registration | Create | PreOperation
    /// Format AD-EN-DDMM-0001 (decision D4: one global running number). Also sets Registration Date if blank.
    /// There is no alternate key, so uniqueness of the running number is checked here in code.
    /// </summary>
    public class RegistrationConfirmationNumberPlugin : PluginBase
    {
        private const int LookBack = 50;      // newest registrations scanned for the highest running number
        private const int MaxAttempts = 20;   // safety limit for the uniqueness loop

        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];

            // AD = first letter of first name + first letter of last name (X if blank).
            string firstLetter = "X";
            string secondLetter = "X";
            var attendeeRef = target.GetAttributeValue<EntityReference>(M3Names.RegAttendee);
            if (attendeeRef != null)
            {
                var attendee = service.Retrieve(attendeeRef.LogicalName, attendeeRef.Id,
                    new ColumnSet(M3Names.AttFirstName, M3Names.AttLastName));
                firstLetter = FirstLetter(attendee.GetAttributeValue<string>(M3Names.AttFirstName));
                secondLetter = FirstLetter(attendee.GetAttributeValue<string>(M3Names.AttLastName));
            }

            string prefix = firstLetter + secondLetter + "-EN-" + DateTime.UtcNow.ToString("ddMM") + "-";

            // Next running number = highest recent number + 1; skip forward if that number is already taken.
            int next = GetHighestRunningNumber(service) + 1;
            int attempts = 0;
            while (RunningNumberExists(service, next) && attempts < MaxAttempts)
            {
                next++;
                attempts++;
            }

            target[M3Names.RegConfirmationNumber] = prefix + next.ToString("0000");

            if (!target.Contains(M3Names.RegDate) || target[M3Names.RegDate] == null)
            {
                target[M3Names.RegDate] = DateTime.UtcNow;
            }
        }

        private static string FirstLetter(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "X" : value.Trim().Substring(0, 1).ToUpperInvariant();
        }

        // Reads the newest registrations and returns the highest running number found (0 if none).
        // Scanning several records means one badly formatted number cannot reset the sequence to 1.
        private static int GetHighestRunningNumber(IOrganizationService service)
        {
            var query = new QueryExpression(M3Names.Registration)
            {
                TopCount = LookBack,
                ColumnSet = new ColumnSet(M3Names.RegConfirmationNumber)
            };
            query.Criteria.AddCondition(M3Names.RegConfirmationNumber, ConditionOperator.NotNull);
            query.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

            int highest = 0;
            foreach (var record in service.RetrieveMultiple(query).Entities)
            {
                string value = record.GetAttributeValue<string>(M3Names.RegConfirmationNumber);
                int running;
                if (!string.IsNullOrEmpty(value)
                    && int.TryParse(value.Substring(value.LastIndexOf('-') + 1), out running)
                    && running > highest)
                {
                    highest = running;
                }
            }
            return highest;
        }

        private static bool RunningNumberExists(IOrganizationService service, int number)
        {
            var query = new QueryExpression(M3Names.Registration)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet(false)
            };
            query.Criteria.AddCondition(M3Names.RegConfirmationNumber, ConditionOperator.EndsWith, "-" + number.ToString("0000"));
            return service.RetrieveMultiple(query).Entities.Any();
        }
    }
}
