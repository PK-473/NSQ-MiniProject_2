/**
* NSQ.Event | Table: Event (trpj_event) | Web resource: trpj_/scripts/event.js
* Owner: M1 (Taneem)
* Version: 1.0.0  (see docs/version-history/JavaScript-VersionHistory.md)
*/
var NSQ = NSQ || {};
 
NSQ.Event = (function () {
    "use strict";
 
    var FIELD = {
        startDate: "trpj_startdate",
        endDate: "trpj_enddate"
    };
 
    // E1: End Date read-only until Start Date is set; End <= Start shows an error and clears End.
    function onDateChange(executionContext) {
        var formContext = executionContext.getFormContext();
        var start = formContext.getAttribute(FIELD.startDate).getValue();
        var endAttribute = formContext.getAttribute(FIELD.endDate);
        var endControl = formContext.getControl(FIELD.endDate);
 
        endControl.setDisabled(start === null);
 
        var end = endAttribute.getValue();
        if (start !== null && end !== null && end <= start) {
            endAttribute.setValue(null);
            Xrm.Navigation.openAlertDialog({
                text: "End Date must be later than Start Date.",
                title: "Invalid End Date"
            });
        }
    }

    // E7: Start Date cannot be in the past when creating a new event.
    function onSave(executionContext) {
        var formContext = executionContext.getFormContext();
        var FORM_CREATE = 1;
        if (formContext.ui.getFormType() !== FORM_CREATE) { return; }   // only on create
 
        var start = formContext.getAttribute(FIELD.startDate).getValue();
        if (start !== null && start < new Date()) {
            executionContext.getEventArgs().preventDefault();           // stops the save
            Xrm.Navigation.openAlertDialog({
                text: "Start Date cannot be in the past.",
                title: "Invalid Start Date"
            });
        }
    }

    // E11: Event Type cannot be changed once at least one session exists.
    // Queries the Session table directly, because the Number of Sessions rollup is not instant.
    function lockEventTypeIfSessionsExist(formContext) {
        var FORM_CREATE = 1;
        if (formContext.ui.getFormType() === FORM_CREATE) { return; }   // a new event has no sessions yet
 
        var eventId = formContext.data.entity.getId().replace(/[{}]/g, "");
        var query = "?$select=trpj_sessionid&$top=1&$filter=_trpj_eventid_value eq " + eventId;
 
        Xrm.WebApi.retrieveMultipleRecords("trpj_session", query).then(
            function (result) {
                if (result.entities.length > 0) {
                    formContext.getControl("trpj_eventtype").setDisabled(true);
                }
            },
            function (error) {
                console.log("E11 session check failed: " + error.message);
            }
        );
    }

    // E12: switch the BPF to match Event Type. The BPF is found by name, so no GUID is hardcoded.
    var EVENT_TYPE = { conference: 100000000, webinar: 100000001, workshop: 100000002, corporate: 100000003 };
 
    function switchProcessByEventType(formContext) {
        var type = formContext.getAttribute("trpj_eventtype").getValue();
        if (type === null) { return; }
 
        var name = (type === EVENT_TYPE.corporate) ? "Corporate Event Process" : "Public Event Process";
        var query = "?$select=workflowid&$top=1&$filter=name eq '" + name + "' and category eq 4 and statecode eq 1";
 
        Xrm.WebApi.retrieveMultipleRecords("workflow", query).then(function (result) {
            if (result.entities.length === 0) { return; }
 
            var processId = result.entities[0].workflowid.toLowerCase();
            var active = formContext.data.process.getActiveProcess();
            if (active && active.getId().replace(/[{}]/g, "").toLowerCase() === processId) { return; }   // already on it
 
            formContext.data.process.setActiveProcess(processId, function () { });
        });
    }
 
       function descriptionRequiredFor(type) {
        return type === EVENT_TYPE.conference || type === EVENT_TYPE.workshop;
    }
 
    function onEventTypeChange(executionContext) {
        var formContext = executionContext.getFormContext();
        var FORM_CREATE = 1;
        var type = formContext.getAttribute("trpj_eventtype").getValue();
 
        if (formContext.ui.getFormType() === FORM_CREATE) {
            // ...your existing auto-save code for new records stays here...
            return;
        }
 
        // Saved record: if the new type needs a Description and it is empty, wait for a successful save.
        var description = formContext.getAttribute("trpj_description").getValue();
        if (descriptionRequiredFor(type) && !description) {
            formContext.ui.setFormNotification(
                "Enter a Description and save to switch the process.", "INFO", "bpfPending");
            return;
        }
        formContext.ui.clearFormNotification("bpfPending");
        switchProcessByEventType(formContext);
    }
 
    // Runs after every successful save, so the switch happens once the required Description exists.
    function onPostSave(executionContext) {
        var formContext = executionContext.getFormContext();
        formContext.ui.clearFormNotification("bpfPending");
        switchProcessByEventType(formContext);
    }

    // S4: warn when the event has no sessions and starts in less than 24 hours.
    function warnIfNoSessionsSoon(formContext) {
        var FORM_CREATE = 1;
        var NOTIFICATION_ID = "noSessionWarning";
        formContext.ui.clearFormNotification(NOTIFICATION_ID);
 
        if (formContext.ui.getFormType() === FORM_CREATE) { return; }   // a new event has no ID to query yet
 
        var start = formContext.getAttribute("trpj_startdate").getValue();
        if (start === null) { return; }
 
        var hoursAway = (start - new Date()) / 3600000;
        if (hoursAway <= 0 || hoursAway >= 24) { return; }              // only when it starts within the next 24 hours
 
        var eventId = formContext.data.entity.getId().replace(/[{}]/g, "");
        var query = "?$select=trpj_sessionid&$top=1&$filter=_trpj_eventid_value eq " + eventId;
 
        Xrm.WebApi.retrieveMultipleRecords("trpj_session", query).then(
            function (result) {
                if (result.entities.length === 0) {
                    formContext.ui.setFormNotification(
                        "No sessions exist and this event starts in less than 24 hours.",
                        "WARNING", NOTIFICATION_ID);
                }
            },
            function (error) {
                console.log("S4 session check failed: " + error.message);
            }
        );
    }
 
    function onLoad(executionContext) {
        var formContext = executionContext.getFormContext();
        formContext.data.entity.addOnPostSave(onPostSave);
        onDateChange(executionContext);   // E1
        lockEventTypeIfSessionsExist(formContext);  // E11
        warnIfNoSessionsSoon(formContext);  // S4
        switchProcessByEventType(formContext);  // E12
    }
 
    return {
        onLoad: onLoad,
        onDateChange: onDateChange,
        onSave: onSave,
        onEventTypeChange: onEventTypeChange
    };
})();
