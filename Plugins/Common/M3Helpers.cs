using Microsoft.Xrm.Sdk;

namespace NSQ.EventManagement.Plugins.Common
{
    /// <summary>
    /// Small helpers used by M3's plugins (Registration and Attendee).
    /// Kept separate so the shared PluginBase.cs does not need to change.
    /// </summary>
    internal static class M3Helpers
    {
        // On Update, Target holds only changed columns: read the new value if present, else the pre-image.
        public static T GetValue<T>(Entity target, Entity preImage, string attribute)
        {
            if (target.Contains(attribute)) { return target.GetAttributeValue<T>(attribute); }
            return preImage != null ? preImage.GetAttributeValue<T>(attribute) : default(T);
        }
    }
}
