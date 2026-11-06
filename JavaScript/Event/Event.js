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
 
    function onLoad(executionContext) {
        onDateChange(executionContext);   // E1
        // The next validations (E7, E11, S4, E12, E2) get added here, one at a time.
    }
 
    return {
        onLoad: onLoad,
        onDateChange: onDateChange
    };
})();
