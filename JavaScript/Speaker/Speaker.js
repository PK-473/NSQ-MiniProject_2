
//Validation : 2

var Speaker = Speaker || {};

Speaker.validateEmailAndPhone = function (executionContext) {

    var formContext = executionContext.getFormContext();

    var email = formContext.getAttribute("emailaddress")?.getValue();
    var phone = formContext.getAttribute("trpj_phone_number")?.getValue();

    // Email Validation
    if (email) {
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!emailRegex.test(email)) {
            formContext.getControl("emailaddress")
                .setNotification("Please enter a valid email address.");
        }
        else {
            formContext.getControl("emailaddress")
                .clearNotification();
        }
    }

    // Phone Validation
    if (phone) {

        var phoneRegex = /^\+\d+$/;

        if (!phoneRegex.test(phone)) {

            formContext.getControl("trpj_phone_number")
                .setNotification("Phone number must start with '+' and contain only digits.");
        }
        else {
            formContext.getControl("trpj_phone_number")
                .clearNotification();
        }
    }
};







//Validation : 3

Speaker.validateLinkedInUrl = function (executionContext) {

    var formContext = executionContext.getFormContext();

    var linkedInUrl =
        formContext.getAttribute("trpj_linkedin_profile")?.getValue();

    var control =
        formContext.getControl("trpj_linkedin_profile");

    // Clear old notification
    control.clearNotification("linkedinValidation");

    // Blank value is allowed
    if (!linkedInUrl) {
        return;
    }

    var linkedInRegex =
        /^https?:\/\/(www\.)?linkedin\.com\/.*$/i;

    if (!linkedInRegex.test(linkedInUrl)) {

        control.setNotification(
            "Please enter a valid LinkedIn Profile URL.",
            "linkedinValidation"
        );
    }
};




//Validation : 4

Speaker.validateContactInfo = function (executionContext) {

    var formContext = executionContext.getFormContext();
    var eventArgs = executionContext.getEventArgs();

    var email =
        formContext.getAttribute("emailaddress")?.getValue();

    var phone =
        formContext.getAttribute("trpj_phone_number")?.getValue();

    // Clear previous notification
    formContext.ui.clearFormNotification("contactValidation");

    if (!email && !phone) {

        formContext.ui.setFormNotification(
            "Either Email Address or Phone Number must be provided.",
            "ERROR",
            "contactValidation"
        );

        eventArgs.preventDefault();
    }
};