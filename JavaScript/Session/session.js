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