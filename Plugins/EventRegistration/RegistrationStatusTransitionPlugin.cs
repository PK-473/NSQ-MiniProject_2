using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    /// <summary>
    /// R7 | Event Registration | Update | PreValidation
    /// Filtering: trpj_registrationstatus | Pre-image (alias PreImage): trpj_registrationstatus
    ///
    /// Status only moves forward, and Registered is the starting status:
    ///   Registered to Cancelled, Attended or No Show      allowed
    ///   No Show to Attended (a correction)                allowed
    ///   Cancelled is final                                any change blocked
    ///   Attended is final                                 any change blocked
    ///   anything back to Registered                       blocked
    /// This closes the loophole Cancelled to No Show or Attended, then back to Registered.
    /// </summary>
    public class RegistrationStatusTransitionPlugin : PluginBase
    {
        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];
            if (!target.Contains(M3Names.RegStatus)) { return; }

            var newStatus = target.GetAttributeValue<OptionSetValue>(M3Names.RegStatus);
            if (newStatus == null) { return; }

            var oldStatus = GetOldStatus(context, service, target);
            if (oldStatus == null || oldStatus.Value == newStatus.Value) { return; }   // nothing changed

            int from = oldStatus.Value;
            int to = newStatus.Value;

            if (from == M3Names.RegStatusCancelled)
            {
                if (to == M3Names.RegStatusRegistered)
                {
                    throw new InvalidPluginExecutionException("A cancelled registration cannot be changed back to Registered.");
                }
                throw new InvalidPluginExecutionException("A cancelled registration is final and cannot be changed to another status.");
            }

            if (from == M3Names.RegStatusAttended)
            {
                throw new InvalidPluginExecutionException("An attended registration is final and its status cannot be changed.");
            }

            if (from == M3Names.RegStatusNoShow && to != M3Names.RegStatusAttended)
            {
                throw new InvalidPluginExecutionException("A No Show registration can only be corrected to Attended.");
            }

            // From Registered, every other status is allowed.
        }

        // Uses the pre-image when it exists. If the image was not registered, read the stored value:
        // at PreValidation the database still holds the old status, so the rule never silently turns off.
        private static OptionSetValue GetOldStatus(IPluginExecutionContext context, IOrganizationService service, Entity target)
        {
            var preImage = RegistrationHelper.GetPreImage(context);
            var fromImage = preImage != null ? preImage.GetAttributeValue<OptionSetValue>(M3Names.RegStatus) : null;
            if (fromImage != null) { return fromImage; }

            var current = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(M3Names.RegStatus));
            return current.GetAttributeValue<OptionSetValue>(M3Names.RegStatus);
        }
    }
}