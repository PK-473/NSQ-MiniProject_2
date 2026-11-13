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

    // E1: Maintain End Date availability based on Start Date.
    function updateEndDateAvailability(formContext) {
        var start = formContext.getAttribute(FIELD.startDate).getValue();
        var endControl = formContext.getControl(FIELD.endDate);
 
        if (endControl) {
            endControl.setDisabled(start === null);
        }
    }
 
    // E1: End Date read-only until Start Date is set.
    // Earlier calendar dates show an error.
    // Same calendar date is allowed for time adjustment.
    // Final date-time validation happens on Save.
    function onDateChange(executionContext) {
        var formContext = executionContext.getFormContext();
 
        var start = formContext.getAttribute(FIELD.startDate).getValue();
        var endAttribute = formContext.getAttribute(FIELD.endDate);
 
        updateEndDateAvailability(formContext);
 
        var end = endAttribute.getValue();
 
        var notificationId = "E1_DateValidation";
 
        if (start === null || end === null) {
            formContext.ui.clearFormNotification(notificationId);
            return;
        }
    
        // Compare calendar dates only during field changes.
        var startDay = new Date(
            start.getFullYear(),
            start.getMonth(),
            start.getDate()
        );
 
        var endDay = new Date(
            end.getFullYear(),
            end.getMonth(),
            end.getDate()
        );
 
        if (endDay < startDay) {
            formContext.ui.setFormNotification(
                "End Date cannot be earlier than Start Date.",
                "ERROR",
            notificationId
        );
        } else {
            formContext.ui.clearFormNotification(notificationId);
        }
    }


     var EVENT_STATUS = { draft: 100000000, scheduled: 100000001, completed: 100000002, cancelled: 100000003 };
 
    // E2: when the event is Completed or Cancelled, every field on the form becomes read-only.
    // keepStatusEditable is true right after the user changes the status, so they can still correct a mistake before saving.
       var lockedByE2 = [];   // names of the controls E2 disabled, so only those get unlocked later
 
    // E2: Completed or Cancelled makes every field read-only; going back to Draft or Scheduled unlocks them again.
    function lockFormIfClosed(formContext, keepStatusEditable) {
        var status = formContext.getAttribute("trpj_eventstatus").getValue();
        var closed = (status === EVENT_STATUS.completed || status === EVENT_STATUS.cancelled);
 
        if (!closed) {
            unlockFormAfterClose(formContext);
            return;
        }
 
        formContext.ui.controls.forEach(function (control) {
            if (typeof control.setDisabled !== "function") { return; }   // skip timeline, subgrids
 
            if (keepStatusEditable && typeof control.getAttribute === "function") {
                var attribute = control.getAttribute();
                if (attribute && attribute.getName() === "trpj_eventstatus") { return; }
            }
            if (!control.getDisabled()) {                                 // only lock what is currently editable
                control.setDisabled(true);
                lockedByE2.push(control.getName());
            }
        });
    }
 
    function unlockFormAfterClose(formContext) {
        lockedByE2.forEach(function (name) {
            var control = formContext.getControl(name);
 
            if (control) {
                control.setDisabled(false);
            }
        });
    
        lockedByE2 = [];
 
        // Reapply E1 after E2 unlocks the form.
        updateEndDateAvailability(formContext);
 
        lockEventTypeIfSessionsExist(formContext); // E11
    }
 
 
    function onStatusChange(executionContext) {
        lockFormIfClosed(executionContext.getFormContext(), true);
    }

    // E1 and E7: Validate Event dates before saving.
    function onSave(executionContext) {
        var formContext = executionContext.getFormContext();
 
        var start = formContext.getAttribute(FIELD.startDate).getValue();
        var end = formContext.getAttribute(FIELD.endDate).getValue();
 
        var notificationId = "E1_DateValidation";
 
        // E1: End Date and Time must be later than Start Date and Time.
        if (start !== null && end !== null && end.getTime() <= start.getTime()) {
 
            formContext.ui.setFormNotification(
                "End Date and Time must be later than Start Date and Time.",
                "ERROR",
                notificationId
            );
 
            executionContext.getEventArgs().preventDefault();
 
            return;
        }
 
        // Clear E1 error when dates are valid.
        formContext.ui.clearFormNotification(notificationId);
 
        // E7: Start Date cannot be in the past when creating a new event.
        var FORM_CREATE = 1;
 
        if (formContext.ui.getFormType() !== FORM_CREATE) {
            return;
        }
 
        if (start !== null && start < new Date()) {
 
            executionContext.getEventArgs().preventDefault();
 
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
        lockFormIfClosed(formContext, false);
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
        lockFormIfClosed(formContext, false);  // E2
    }
 
    return {
        onLoad: onLoad,
        onDateChange: onDateChange,
        onSave: onSave,
        onEventTypeChange: onEventTypeChange,
        onStatusChange: onStatusChange

    };
})();
