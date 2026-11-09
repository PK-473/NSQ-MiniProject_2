using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSQ.EventManagement.Plugins.Common;

namespace NSQ.EventManagement.Plugins.EventRegistration
{
    // Shared helper for the registration classes.
    internal static class RegistrationHelper
    {
        public static Entity GetPreImage(IPluginExecutionContext context)
        {
            return context.PreEntityImages.Contains(M3Names.PreImageAlias)
                ? context.PreEntityImages[M3Names.PreImageAlias]
                : null;
        }
    }
}
