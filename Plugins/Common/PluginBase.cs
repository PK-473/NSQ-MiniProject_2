using System;
using Microsoft.Xrm.Sdk;

namespace NSQ.EventManagement.Plugins.Common
{
    /*
     * Shared base class for all plug-ins.
     *
     * This class contains the common Dataverse setup required by every
     * plug-in, including context, tracing, organization service and
     * exception handling.
     */
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Get the plug-in execution information.
            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(
                    typeof(IPluginExecutionContext));

            // Get the tracing service for Plugin Trace Log messages.
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(
                    typeof(ITracingService));

            // Get the service factory.
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(
                    typeof(IOrganizationServiceFactory));

            // Create the Dataverse organization service.
            IOrganizationService service =
                serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                // Run the business logic written in the child plug-in class.
                ExecutePlugin(context, service, tracingService);
            }
            catch (InvalidPluginExecutionException)
            {
                // Keep intentional business-validation messages unchanged.
                throw;
            }
            catch (Exception ex)
            {
                // Record technical details in the Plugin Trace Log.
                tracingService.Trace(
                    "Unexpected plug-in error: {0}",
                    ex.ToString());

                // Show a controlled message instead of a raw exception.
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred while processing the request.",
                    ex);
            }
        }

        /*
         * Every child plug-in must override this method.
         * Only requirement-specific business logic belongs in the child class.
         */
        protected abstract void ExecutePlugin(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService);
    }
}