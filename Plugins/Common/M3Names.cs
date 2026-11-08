namespace NSQ.EventManagement.Plugins.Common
{
    /// <summary>
    /// Logical names and choice values used by M3's plugins (Registration and Attendee).
    /// Prefix must match the NSQ Event Management publisher. Choice values were read from Env 1: Registration Status 738000000+, Event Status 100000000+. Re-check with getOptions() if a column changes.
    /// </summary>
    internal static class M3Names
    {
        public const string Prefix = "trpj_";

        // Event (read only by M3 plugins). Event Status is a LOCAL choice whose values M1 set to the 100000000 series.
        public const string EventStatus = Prefix + "eventstatus";
        public const string EventStartDate = Prefix + "startdate";
        public const int EventStatusCompleted = 100000002;
        public const int EventStatusCancelled = 100000003;

        // Event Registration. Registration Status is a GLOBAL choice in the 738000000 series.
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