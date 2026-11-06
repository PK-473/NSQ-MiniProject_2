/**
 * NSQ.Attendee | Table: Attendee (trpj_attendee) | Web resource: trpj_/scripts/attendee.js
 * Owner: M3
 * Version: 1.0.0   (history: docs/version-history/JavaScript-VersionHistory.md)
 * Items: A2, A3, A4, A7 + Full Name builder.
 * A5 and A6 are implemented as Business Rules, not in this file.
 */
var NSQ = NSQ || {};
NSQ.Attendee = (function () {
    "use strict";

    // Change this one value to your publisher prefix (for example "ptrj_").
    var P = "trpj_";

    var FIELD = {
        fullName: P + "fullname",
        firstName: P + "firstname",
        lastName: P + "lastname",
        email: P + "email",
        phone: P + "phone",
        contactMethod: P + "preferredcontactmethod"
    };

    // Same numbers as the guide, section 5.5.
    var CONTACT_METHOD = { email: 100000000, phone: 100000001, both: 100000002 };

    var EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    var PHONE_REGEX = /^\+\d{7,15}$/;   // starts with +, then digits only
    var DIGIT_REGEX = /\d/;

    // ---------- small helpers ----------
    function getValue(formContext, field) {
        var attr = formContext.getAttribute(field);
        return attr ? attr.getValue() : null;
    }

    function isBlank(value) {
        return value === null || value === undefined || String(value).trim() === "";
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
        var parts = [getValue(formContext, FIELD.firstName), getValue(formContext, FIELD.lastName)]
            .filter(function (v) { return !isBlank(v); })
            .map(function (v) { return String(v).trim(); });
        var fullName = parts.join(" ");
        var attr = formContext.getAttribute(FIELD.fullName);
        if (attr && attr.getValue() !== (fullName || null)) {
            attr.setValue(fullName || null);
        }
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
                clearFieldError(formContext, field, id);
            }
        });
        return valid;
    }

    // ---------- A2: required fields depend on Preferred Contact Method ----------
    function applyContactMethodRules(formContext) {
        var method = getValue(formContext, FIELD.contactMethod);
        var needEmail = (method === CONTACT_METHOD.email || method === CONTACT_METHOD.both);
        var needPhone = (method === CONTACT_METHOD.phone || method === CONTACT_METHOD.both);
        formContext.getAttribute(FIELD.email).setRequiredLevel(needEmail ? "required" : "none");
        formContext.getAttribute(FIELD.phone).setRequiredLevel(needPhone ? "required" : "none");
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
            if (!PHONE_REGEX.test(cleaned)) {
                setFieldError(formContext, FIELD.phone, "Phone must start with + and contain only numbers, for example +919876543210.", "A7_phone");
                valid = false;
            } else {
                clearFieldError(formContext, FIELD.phone, "A7_phone");
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
        formContext.ui.clearFormNotification("A3");
        return true;
    }

    // ---------- event handlers ----------
    function onLoad(executionContext) {
        var formContext = executionContext.getFormContext();
        var fullNameControl = formContext.getControl(FIELD.fullName);
        if (fullNameControl) { fullNameControl.setDisabled(true); }
        // A disabled field is still saved when submit mode is "always".
        formContext.getAttribute(FIELD.fullName).setSubmitMode("always");
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
        validateFormats(executionContext.getFormContext());           // A7
    }

    function onSave(executionContext) {
        var formContext = executionContext.getFormContext();
        buildFullName(formContext);
        // Run every check so all messages show together.
        var namesOk = validateNames(formContext);           // A4
        var formatsOk = validateFormats(formContext);       // A7
        var contactOk = validateContactPresent(formContext); // A3
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
