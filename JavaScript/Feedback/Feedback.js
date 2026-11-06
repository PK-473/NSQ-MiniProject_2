/*
Requirement F1:
Show Comments only when Rating is 1 or 2.
Hide Comments when Rating is blank, 3, 4 or 5.

Note:
The Business Rule controls whether Comments is required.
*/

function showHideComments(executionContext) {

    // Get the current Feedback form
    var formContext = executionContext.getFormContext();

    // Get the selected Rating value
    var rating = formContext.getAttribute("trpj_rating").getValue();

    // Get the Comments field control
    var commentsControl = formContext.getControl("trpj_comments");

    // If Rating is 1 or 2, show Comments and allow editing
    if (rating === 1 || rating === 2) {
        commentsControl.setVisible(true);
        commentsControl.setDisabled(false);
    }

    // For blank, 3, 4 or 5, hide Comments
    else {
        commentsControl.setVisible(false);
    }
}


/*
Requirement F7:
Submitted On should remain read-only after record creation.

New record      = Submitted On is editable
Existing record = Submitted On is read-only
*/
function lockSubmittedOn(executionContext) {

    // Get the current Feedback form
    var formContext = executionContext.getFormContext();

    // Get the current form type
    var formType = formContext.ui.getFormType();

    // Get the Submitted On field control
    var submittedOnControl =
        formContext.getControl("trpj_submittedon");

    // Stop the function if Submitted On is not present on the form
    if (submittedOnControl === null) {
        return;
    }

    // Form type 1 means Create form
    if (formType === 1) {

        // Keep Submitted On editable on a new Feedback record
        submittedOnControl.setDisabled(false);
    }
    else {

        // Lock Submitted On for an existing Feedback record
        submittedOnControl.setDisabled(true);
    }
}