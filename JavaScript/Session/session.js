var Session = Session || {};

Session.validateSessionDates = async function (executionContext) {

    var formContext = executionContext.getFormContext();
    var eventArgs = executionContext.getEventArgs();

    formContext.ui.clearFormNotification("sessionDateValidation");

    var eventLookup = formContext.getAttribute("trpj_eventid")?.getValue();

    var sessionStart = formContext.getAttribute("trpj_start_time")?.getValue();

    var sessionEnd = formContext.getAttribute("trpj_end_date")?.getValue();


    if (sessionEnd <= sessionStart) {

    formContext.ui.setFormNotification(
        "Session End Date must be greater than Session Start Time.",
        "ERROR",
        "sessionDateValidation"
    );

    eventArgs.preventDefault();
    return;
}

    // Validation only if all values exist
    if (!eventLookup || !sessionStart || !sessionEnd) {
        return;
    }

    try {

        var eventId = eventLookup[0].id.replace("{", "").replace("}", "");

        var eventRecord = await Xrm.WebApi.retrieveRecord(
            "trpj_event",
            eventId,
            "?$select=trpj_startdate,trpj_enddate"
        );

        var eventStart = new Date(eventRecord.trpj_startdate);
        var eventEnd = new Date(eventRecord.trpj_enddate);

        if (sessionStart < eventStart) {

    formContext.ui.setFormNotification(
        "Session Start Time cannot be earlier than Event Start Date.",
        "ERROR",
        "sessionDateValidation"
    );

    eventArgs.preventDefault();
    return;
}

if (sessionEnd > eventEnd) {

    formContext.ui.setFormNotification(
        "Session End Date cannot be later than Event End Date.",
        "ERROR",
        "sessionDateValidation"
    );

    eventArgs.preventDefault();
    return;
}

    }
    catch (error) {

        console.log(error);

        formContext.ui.setFormNotification(
            "Unable to validate Event dates.",
            "ERROR",
            "sessionDateValidation"
        );

        eventArgs.preventDefault();
    }
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

    var startDate = formContext.getAttribute("trpj_start_time")?.getValue();

    var endDate = formContext.getAttribute("trpj_end_date")?.getValue();

    var endDateControl = formContext.getControl("trpj_end_date");

    if (!startDate || !endDate) {
        endDateControl.clearNotification("endDateValidation");
        return;
    }

    // End Date must be greater than Start Time
    if (endDate <= startDate) {

        endDateControl.setNotification(
            "End Date must be greater than Start Time.",
            "endDateValidation"
        );

        // Clear invalid value
        formContext.getAttribute("trpj_end_date").setValue(null);

        return;
    }

    endDateControl.clearNotification("endDateValidation");
};