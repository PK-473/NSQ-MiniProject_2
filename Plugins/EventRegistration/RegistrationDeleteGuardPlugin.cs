using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    /// <summary>
    /// R8 | Event Registration | Delete | PreValidation | no pre-image: the record is retrieved.
    /// </summary>
    public class RegistrationDeleteGuardPlugin : PluginBase
    {
        protected override void ExecutePlugin(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var targetRef = (EntityReference)context.InputParameters["Target"];
            var registration = service.Retrieve(targetRef.LogicalName, targetRef.Id, new ColumnSet(M3Names.RegStatus));

            var status = registration.GetAttributeValue<OptionSetValue>(M3Names.RegStatus);
            if (status != null && status.Value == M3Names.RegStatusAttended)
            {
                throw new InvalidPluginExecutionException("A registration cannot be deleted once the attendee is marked Attended.");
            }
        }
    }
}
