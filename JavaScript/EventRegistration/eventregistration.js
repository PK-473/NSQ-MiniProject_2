/**
 * NSQ.EventRegistration | Table: Event Registration (trpj_eventregistration)
 * Web resource: trpj_/scripts/eventregistration.js
 * Owner: M3
 * Version: 1.0.0   (history: docs/version-history/JavaScript-VersionHistory.md)
 * Items: R1
 */
var NSQ = NSQ || {};
NSQ.EventRegistration = (function () {
    "use strict";

    // Change this one value to your publisher prefix (for example "ptrj_").
    var P = "trpj_";

    var FIELD = {
        event: P + "eventid",
        eventStart: P + "eventstartdate",
        eventEnd: P + "eventenddate",
        confirmationNumber: P + "confirmationnumber"
    };

    // Columns on the Event table that R1 copies.
    var EVENT_COLUMN = { start: P + "startdate", end: P + "enddate" };

    function setDateValue(formContext, field, value) {
        var attr = formContext.getAttribute(field);
        if (attr) { attr.setValue(value); }
    }

    // R1: fill Event Start/End Date from the selected Event; clear both when the Event is removed.
    function copyEventDates(formContext) {
        var eventValue = formContext.getAttribute(FIELD.event).getValue();

        if (eventValue === null || eventValue.length === 0) {
            setDateValue(formContext, FIELD.eventStart, null);
            setDateValue(formContext, FIELD.eventEnd, null);
            return;
        }

        var eventRef = eventValue[0];
        var eventId = eventRef.id.replace(/[{}]/g, "");
        var options = "?$select=" + EVENT_COLUMN.start + "," + EVENT_COLUMN.end;

        Xrm.WebApi.retrieveRecord(eventRef.entityType, eventId, options).then(
            function (result) {
                var start = result[EVENT_COLUMN.start];
                var end = result[EVENT_COLUMN.end];
                setDateValue(formContext, FIELD.eventStart, start ? new Date(start) : null);
                setDateValue(formContext, FIELD.eventEnd, end ? new Date(end) : null);
            },
            function (error) {
                Xrm.Navigation.openErrorDialog({ message: "Could not read the Event dates: " + error.message });
            }
        );
    }

    function lockAutoFilledFields(formContext) {
        [FIELD.eventStart, FIELD.eventEnd, FIELD.confirmationNumber].forEach(function (field) {
            var control = formContext.getControl(field);
            if (control) { control.setDisabled(true); }
            var attr = formContext.getAttribute(field);
            if (attr) { attr.setSubmitMode("always"); }
        });
    }

    // Fallback from the guide (section 7.4): if the primary column cannot be made optional,
    // put a temporary value on create. Plugin R10 always overwrites it before the save.
    function setTemporaryConfirmationNumber(formContext) {
        var isCreate = formContext.ui.getFormType() === 1;
        var attr = formContext.getAttribute(FIELD.confirmationNumber);
        if (isCreate && attr && attr.getValue() === null) {
            attr.setValue("AUTO");
        }
    }

    function onLoad(executionContext) {
        var formContext = executionContext.getFormContext();
        lockAutoFilledFields(formContext);
        setTemporaryConfirmationNumber(formContext);
    }

    function onEventChange(executionContext) {
        copyEventDates(executionContext.getFormContext());   // R1
    }

    return { onLoad: onLoad, onEventChange: onEventChange };
})();
