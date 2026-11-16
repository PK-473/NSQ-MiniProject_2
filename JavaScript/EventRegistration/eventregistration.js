/**
 * NSQ.EventRegistration | Table: Event Registration (trpj_eventregistration)
 * Web resource: trpj_/scripts/eventregistration.js
 * Owner: Prathamesh
 * Version: 1.2.0   (history: docs/version-history/JavaScript-VersionHistory.md)
 * Items: R1
 */
var NSQ = NSQ || {};
NSQ.EventRegistration = (function () {
    "use strict";

    // Publisher prefix of NSQ Event Management (must match the solution publisher).
    var P = "trpj_";

    var FIELD = {
        event: P + "eventid",
        eventStart: P + "eventstartdate",
        eventEnd: P + "eventenddate",
        confirmationNumber: P + "confirmationnumber"
    };

    // Columns on the Event table that R1 copies.
    var EVENT_COLUMN = { start: P + "startdate", end: P + "enddate" };

    var FORM_TYPE_CREATE = 1;

    function setDateValue(formContext, field, value) {
        var attr = formContext.getAttribute(field);
        if (attr) { attr.setValue(value); }
    }

    // Returns the selected Event's id (lowercase, no braces) or null.
    function getSelectedEventId(formContext) {
        var attr = formContext.getAttribute(FIELD.event);
        var value = attr ? attr.getValue() : null;
        if (value === null || value.length === 0) { return null; }
        return value[0].id.replace(/[{}]/g, "").toLowerCase();
    }

    // R1: fill Event Start/End Date from the selected Event; clear both when the Event is removed.
    function copyEventDates(formContext) {
        var attr = formContext.getAttribute(FIELD.event);
        if (!attr) { return; }

        var eventId = getSelectedEventId(formContext);
        if (eventId === null) {                       // ELSE branch: Event removed
            setDateValue(formContext, FIELD.eventStart, null);
            setDateValue(formContext, FIELD.eventEnd, null);
            return;
        }

        var entityType = attr.getValue()[0].entityType;
        var options = "?$select=" + EVENT_COLUMN.start + "," + EVENT_COLUMN.end;

        Xrm.WebApi.retrieveRecord(entityType, eventId, options).then(
            function (result) {
                // Ignore a late answer if the user has already picked another Event or cleared it.
                if (getSelectedEventId(formContext) !== eventId) { return; }
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

    // Event Start/End are filled by this script, so they must be submitted even though they are locked.
    // Confirmation Number is written by plugin R10 on the server, so it is only locked, never submitted.
    function lockAutoFilledFields(formContext) {
        [FIELD.eventStart, FIELD.eventEnd, FIELD.confirmationNumber].forEach(function (field) {
            var control = formContext.getControl(field);
            if (control) { control.setDisabled(true); }
        });
        [FIELD.eventStart, FIELD.eventEnd].forEach(function (field) {
            var attr = formContext.getAttribute(field);
            if (attr) { attr.setSubmitMode("always"); }
        });
    }

    function onLoad(executionContext) {
        var formContext = executionContext.getFormContext();
        lockAutoFilledFields(formContext);
        // A new registration opened from an Event's subgrid arrives with the Event already filled in,
        // and OnChange does not fire for that, so copy the dates now.
        if (formContext.ui.getFormType() === FORM_TYPE_CREATE && getSelectedEventId(formContext) !== null) {
            copyEventDates(formContext);
        }
    }

    function onEventChange(executionContext) {
        copyEventDates(executionContext.getFormContext());   // R1
    }

    return { onLoad: onLoad, onEventChange: onEventChange };
})();
 