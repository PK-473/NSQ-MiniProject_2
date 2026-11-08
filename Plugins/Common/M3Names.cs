namespace NSQ.EventManagement.Plugins.Common
{
    /// <summary>
    /// Logical names and choice values used by M3's plugins (Registration and Attendee).
    /// Prefix must match the NSQ Event Management publisher. Choice values use the Env 1 series 738000000+ in creation order. VERIFY each one with getOptions() on the form.
    /// </summary>
    internal static class M3Names
    {
        public const string Prefix = "trpj_";

        // Event (read only by M3 plugins)
        public const string EventStatus = Prefix + "eventstatus";
        public const string EventStartDate = Prefix + "startdate";
        public const int EventStatusCompleted = 738000002;
        public const int EventStatusCancelled = 738000003;

        // Event Registration
        public const string Registration = Prefix + "eventregistration";
        public const string RegistrationId = Prefix + "eventregistrationid";
        public const string RegEvent = Prefix + "eventid";
        public const string RegAttendee = Prefix + "attendeeid";
        public const string RegStatus = Prefix + "registrationstatus";
        public const string RegDate = Prefix + "registrationdate";
        public const string RegConfirmationNumber = Prefix + "confirmationnumber";
        public const int RegStatusRegistered = 738000000;
        public const int RegStatusCancelled = 738000001;
        public const int RegStatusAttended = 738000002;

        // Attendee
        public const string Attendee = Prefix + "attendee";
        public const string AttendeeId = Prefix + "attendeeid";
        public const string AttFirstName = Prefix + "firstname";
        public const string AttLastName = Prefix + "lastname";
        public const string AttEmail = Prefix + "email";
        public const string AttPhone = Prefix + "phone";

        // Name of the pre-image registered on update steps.
        public const string PreImageAlias = "PreImage";
    }
}
