var Speaker = Speaker || {};

Speaker.validateEmailAndPhone = function (executionContext) {

    var formContext = executionContext.getFormContext();

    var email = formContext.getAttribute("EmailAddress")?.getValue();
    var phone = formContext.getAttribute("trpj_phone_number")?.getValue();

    // Email Validation
    if (email) {
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!emailRegex.test(email)) {
            formContext.getControl("EmailAddress")
                .setNotification("Please enter a valid email address.");
        }
        else {
            formContext.getControl("EmailAddress")
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