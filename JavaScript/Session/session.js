var Session = Session || {};
 
Session.dateCheckPassed = false;   // true only for the second save, after the check passed
 
// S1: the session must fall inside the Event's Start and End Date.
Session.validateSessionDates = function (executionContext) {
 
    var formContext = executionContext.getFormContext();
    var eventArgs = executionContext.getEventArgs();
    var NOTE_ID = "sessionDateValidation";
 
    if (Session.dateCheckPassed) {          // this is the second save, let it through
        Session.dateCheckPassed = false;
        return;
    }
 
    formContext.ui.clearFormNotification(NOTE_ID);
 
    var eventLookup = formContext.getAttribute("trpj_eventid").getValue();
    var sessionStart = formContext.getAttribute("trpj_start_time").getValue();
    var sessionEnd = formContext.getAttribute("trpj_end_date").getValue();
 
    // Nothing to compare: required fields and other rules handle this case.
    if (!eventLookup || !sessionStart || !sessionEnd) { return; }
 
    if (sessionEnd <= sessionStart) {
        formContext.ui.setFormNotification("Session End Date must be greater than Session Start Time.", "ERROR", NOTE_ID);
        eventArgs.preventDefault();
        return;
    }
 
    // The Event dates come from the server, so cancel this save, check, then save again.
    eventArgs.preventDefault();
    var saveMode = eventArgs.getSaveMode();     // 2 = Save and Close, 59 = Save and New
    var eventId = eventLookup[0].id.replace("{", "").replace("}", "");
 
    Xrm.WebApi.retrieveRecord("trpj_event", eventId, "?$select=trpj_startdate,trpj_enddate").then(
        function (eventRecord) {
            var eventStart = new Date(eventRecord.trpj_startdate);
            var eventEnd = new Date(eventRecord.trpj_enddate);
 
            if (sessionStart < eventStart) {
                formContext.ui.setFormNotification("Session Start Time cannot be earlier than Event Start Date.", "ERROR", NOTE_ID);
                return;                          // the save stays cancelled
            }
            if (sessionEnd > eventEnd) {
                formContext.ui.setFormNotification("Session End Date cannot be later than Event End Date.", "ERROR", NOTE_ID);
                return;
            }
 
            Session.dateCheckPassed = true;      // check passed: run the save again
            if (saveMode === 2) { formContext.data.entity.save("saveandclose"); }
            else if (saveMode === 59) { formContext.data.entity.save("saveandnew"); }
            else { formContext.data.save(); }
        },
        function (error) {
            console.log(error);
            formContext.ui.setFormNotification("Unable to validate Event dates.", "ERROR", NOTE_ID);
        }
    );
};





//Validation : 2

var Session = Session || {};

/**
 * Enable End Date only when Start Time is selected
 */
Session.toggleEndDate = function (executionContext) {

    var formContext = executionContext.getFormContext();

    var startDate = formContext.getAttribute("trpj_start_time")?.getValue();

    var endDateControl = formContext.getControl("trpj_end_date");

    if (!endDateControl) {
        return;
    }

    // Disable End Date when Start Time is empty
    if (!startDate) {

        endDateControl.setDisabled(true);

        // Clear End Date if Start Time removed
        formContext.getAttribute("trpj_end_date")?.setValue(null);
    }
    else {
        endDateControl.setDisabled(false);
    }
};


/**
 * Validate End Date against Start Time
 */
Session.validateEndDate = function (executionContext) {

    var formContext = executionContext.getFormContext();

    var startDate =
        formContext.getAttribute("trpj_start_time")?.getValue();

    var endDate =
        formContext.getAttribute("trpj_end_date")?.getValue();

    var endDateControl =
        formContext.getControl("trpj_end_date");

    if (!startDate || !endDate) {

        endDateControl.clearNotification(
            "endDateValidation"
        );

        return;
    }

    // Compare only calendar dates
    var startOnly =
        new Date(
            startDate.getFullYear(),
            startDate.getMonth(),
            startDate.getDate()
        );

    var endOnly =
        new Date(
            endDate.getFullYear(),
            endDate.getMonth(),
            endDate.getDate()
        );

    if (endOnly < startOnly) {

        endDateControl.setNotification(
            "End Date cannot be before Start Date.",
            "endDateValidation"
        );

        return;
    }

    endDateControl.clearNotification(
        "endDateValidation"
    );
};



Session.validateDateTimeOnSave = function (executionContext) {

    var formContext = executionContext.getFormContext();
    var eventArgs = executionContext.getEventArgs();

    var startDate =
        formContext.getAttribute("trpj_start_time")?.getValue();

    var endDate =
        formContext.getAttribute("trpj_end_date")?.getValue();

    formContext.ui.clearFormNotification(
        "dateTimeValidation"
    );

    if (!startDate || !endDate) {
        return;
    }

    if (endDate <= startDate) {

        formContext.ui.setFormNotification(
            "End Date and Time must be greater than Start Date and Time.",
            "ERROR",
            "dateTimeValidation"
        );

        eventArgs.preventDefault();
    }
};



//Validation : 9

/**
 * Session cannot be marked as Completed before its End Time.
 */
Session.validateCompletedStatus = function (executionContext) {

    var formContext = executionContext.getFormContext();
    var eventArgs = executionContext.getEventArgs();

    formContext.ui.clearFormNotification("completedValidation");

    var sessionStatus = formContext.getAttribute("trpj_session_status")?.getValue();

    var sessionEnd = formContext.getAttribute("trpj_end_date")?.getValue();

    var COMPLETED_VALUE = 738000002;

    // Status is not Completed
    if (sessionStatus !== COMPLETED_VALUE) {
        return;
    }

    // Safety check
    if (!sessionEnd) {

        formContext.ui.setFormNotification(
            "End Date is required before marking Session as Completed.",
            "ERROR",
            "completedValidation"
        );

        eventArgs.preventDefault();
        return;
    }

    var currentDateTime = new Date();

    if (currentDateTime < sessionEnd) {

        formContext.ui.setFormNotification(
            "Session cannot be marked as Completed before its End Time.",
            "ERROR",
            "completedValidation"
        );

        eventArgs.preventDefault();
    }
};






//Validation : S12
/**
 * S12
 * Lock Speaker lookup when Session Status = Completed
 */
Session.lockSpeakerWhenCompleted = function (executionContext) {

    var formContext = executionContext.getFormContext();

    var sessionStatus = formContext.getAttribute("trpj_session_status")?.getValue();

    var speakerControl = formContext.getControl("trpj_speakerid");

    if (!speakerControl) {
        return;
    }

    // Replace with actual Completed option value
    var COMPLETED_VALUE = 738000002;

    if (sessionStatus === COMPLETED_VALUE) {

        speakerControl.setDisabled(true);
    }
    else {

        speakerControl.setDisabled(false);
    }
};