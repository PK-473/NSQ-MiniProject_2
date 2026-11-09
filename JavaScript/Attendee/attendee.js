/**
 * NSQ.Attendee | Table: Attendee (trpj_attendee) | Web resource: trpj_/scripts/attendee.js
 * Owner: Prathamesh
 * Version: 1.1.0   (history: docs/version-history/JavaScript-VersionHistory.md)
 * Items: A2, A3, A4, A7 + Full Name builder.
 * A5 and A6 are implemented as Business Rules (each with an ELSE branch), not in this file.
 */
var NSQ = NSQ || {};
NSQ.Attendee = (function () {
    "use strict";

    // Publisher prefix of NSQ Event Management (must match the solution publisher).
    var P = "trpj_";

    var FIELD = {
        fullName: P + "fullname",
        firstName: P + "firstname",
        lastName: P + "lastname",
        email: P + "email",
        phone: P + "phone",
        contactMethod: P + "preferredcontactmethod"
    };

    // Real values of trpj_preferredcontactmethod in Env 1 (the publisher choice prefix is 738).
    var CONTACT_METHOD = { email: 738000000, phone: 738000001, both: 738000002 };

    var MAX_FULL_NAME = 100;               // primary column max length
    var EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    var PHONE_REGEX = /^\+\d{7,15}$/;      // starts with +, then 7 to 15 digits
    var DIGIT_REGEX = /\d/;

    // ---------- small helpers (all null-safe, so a missing field on a form never breaks the script) ----------
    function getValue(formContext, field) {
        var attr = formContext.getAttribute(field);
        return attr ? attr.getValue() : null;
    }

    function isBlank(value) {
        return value === null || value === undefined || String(value).trim() === "";
    }

    function setRequired(formContext, field, required) {
        var attr = formContext.getAttribute(field);
        if (attr) { attr.setRequiredLevel(required ? "required" : "none"); }
    }

    function setFieldError(formContext, field, message, id) {
        var control = formContext.getControl(field);
        if (control) { control.setNotification(message, id); }
    }

    function clearFieldError(formContext, field, id) {
        var control = formContext.getControl(field);
        if (control) { control.clearNotification(id); }
    }

    // ---------- Full Name (primary column) ----------
    // Dataverse needs a primary column, so it is built from First + Last name.
    function buildFullName(formContext) {
        var attr = formContext.getAttribute(FIELD.fullName);
        if (!attr) { return; }
        var parts = [getValue(formContext, FIELD.firstName), getValue(formContext, FIELD.lastName)]
            .filter(function (v) { return !isBlank(v); })
            .map(function (v) { return String(v).trim(); });
        var fullName = parts.join(" ").substring(0, MAX_FULL_NAME) || null;
        if (attr.getValue() !== fullName) { attr.setValue(fullName); }
    }

    // ---------- A4: names must not contain numbers ----------
    function validateNames(formContext) {
        var valid = true;
        [FIELD.firstName, FIELD.lastName].forEach(function (field) {
            var value = getValue(formContext, field);
            var id = "A4_" + field;
            if (!isBlank(value) && DIGIT_REGEX.test(value)) {
                setFieldError(formContext, field, "Names must not contain numbers.", id);
                valid = false;
            } else {
                clearFieldError(formContext, field, id);   // ELSE: error disappears once fixed or cleared
            }
        });
        return valid;
    }

    // ---------- A2: required fields depend on Preferred Contact Method ----------
    // Every branch sets both fields, so switching the method always resets the old requirement.
    function applyContactMethodRules(formContext) {
        var method = getValue(formContext, FIELD.contactMethod);
        var needEmail = (method === CONTACT_METHOD.email || method === CONTACT_METHOD.both);
        var needPhone = (method === CONTACT_METHOD.phone || method === CONTACT_METHOD.both);
        setRequired(formContext, FIELD.email, needEmail);
        setRequired(formContext, FIELD.phone, needPhone);
    }

    // ---------- A7: email format, phone starts with + and is numeric ----------
    function validateFormats(formContext) {
        var valid = true;

        var email = getValue(formContext, FIELD.email);
        if (!isBlank(email) && !EMAIL_REGEX.test(String(email).trim())) {
            setFieldError(formContext, FIELD.email, "Enter a valid email, for example abc@example.com.", "A7_email");
            valid = false;
        } else {
            clearFieldError(formContext, FIELD.email, "A7_email");
        }

        var phone = getValue(formContext, FIELD.phone);
        if (!isBlank(phone)) {
            var cleaned = String(phone).replace(/[\s\-().]/g, "");
            if (PHONE_REGEX.test(cleaned)) {
                clearFieldError(formContext, FIELD.phone, "A7_phone");
                // Store the clean form, so "+91 98765 43210" and "+919876543210" are the same number for plugin A1.
                if (cleaned !== phone) { formContext.getAttribute(FIELD.phone).setValue(cleaned); }
            } else {
                setFieldError(formContext, FIELD.phone, "Phone must start with + and contain only numbers, for example +919876543210.", "A7_phone");
                valid = false;
            }
        } else {
            clearFieldError(formContext, FIELD.phone, "A7_phone");
        }
        return valid;
    }

    // ---------- A3: both Email and Phone missing ----------
    function validateContactPresent(formContext) {
        if (isBlank(getValue(formContext, FIELD.email)) && isBlank(getValue(formContext, FIELD.phone))) {
            formContext.ui.setFormNotification("Enter at least an Email or a Phone number.", "ERROR", "A3");
            return false;
        }
        formContext.ui.clearFormNotification("A3");   // ELSE: message disappears once one is entered
        return true;
    }

    // ---------- event handlers ----------
    function onLoad(executionContext) {
        var formContext = executionContext.getFormContext();
        var fullNameControl = formContext.getControl(FIELD.fullName);
        if (fullNameControl) { fullNameControl.setDisabled(true); }
        var fullNameAttr = formContext.getAttribute(FIELD.fullName);
        // A disabled field is still saved when submit mode is "always".
        if (fullNameAttr) { fullNameAttr.setSubmitMode("always"); }
        buildFullName(formContext);
        applyContactMethodRules(formContext);   // A2
    }

    function onNameChange(executionContext) {
        var formContext = executionContext.getFormContext();
        validateNames(formContext);             // A4
        buildFullName(formContext);
    }

    function onContactMethodChange(executionContext) {
        applyContactMethodRules(executionContext.getFormContext());   // A2
    }

    function onContactDetailChange(executionContext) {
        var formContext = executionContext.getFormContext();
        validateFormats(formContext);                                 // A7
        // Clear the A3 message as soon as one contact detail is entered.
        if (!isBlank(getValue(formContext, FIELD.email)) || !isBlank(getValue(formContext, FIELD.phone))) {
            formContext.ui.clearFormNotification("A3");
        }
    }

    function onSave(executionContext) {
        var formContext = executionContext.getFormContext();
        buildFullName(formContext);
        // Run every check so all messages show together.
        var namesOk = validateNames(formContext);              // A4
        var formatsOk = validateFormats(formContext);          // A7
        var contactOk = validateContactPresent(formContext);   // A3
        if (!(namesOk && formatsOk && contactOk)) {
            executionContext.getEventArgs().preventDefault();
        }
    }

    return {
        onLoad: onLoad,
        onSave: onSave,
        onNameChange: onNameChange,
        onContactMethodChange: onContactMethodChange,
        onContactDetailChange: onContactDetailChange
    };
})();