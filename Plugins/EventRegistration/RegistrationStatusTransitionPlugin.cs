using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    /// <summary>
    /// R7 | Event Registration | Update | PreValidation
    /// Filtering: registrationstatus | Pre-image (alias PreImage): registrationstatus
    /// </summary>
    public class RegistrationStatusTransitionPlugin : PluginBase
    {
        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = (Entity)context.InputParameters["Target"];
            if (!target.Contains(M3Names.RegStatus)) { return; }

            var preImage = RegistrationHelper.GetPreImage(context);
            var oldStatus = preImage != null ? preImage.GetAttributeValue<OptionSetValue>(M3Names.RegStatus) : null;
            var newStatus = target.GetAttributeValue<OptionSetValue>(M3Names.RegStatus);

            if (oldStatus != null && newStatus != null
                && oldStatus.Value == M3Names.RegStatusCancelled
                && newStatus.Value == M3Names.RegStatusRegistered)
            {
                throw new InvalidPluginExecutionException("A cancelled registration cannot be changed back to Registered.");
            }
        }
    }
}
