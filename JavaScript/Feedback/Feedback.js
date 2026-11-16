"use strict";

/*
Requirement F1:
Show Comments only when Rating is 1 or 2.

When Rating changes from 1 or 2 to blank, 3, 4 or 5:
1. Clear Comments
2. Hide Comments

Note:
The F9 Business Rule controls whether Comments is required.
*/

/*
Runs when the Feedback form opens.

This function only shows or hides Comments.
It does not clear existing data during form load.
*/
function showHideCommentsOnLoad(executionContext) {

    // Get the current Feedback form
    var formContext = executionContext.getFormContext();

    // Get Rating and Comments
    var ratingAttribute =
        formContext.getAttribute("trpj_rating");

    var commentsControl =
        formContext.getControl("trpj_comments");

    // Stop safely if fields are unavailable
    if (ratingAttribute === null || commentsControl === null) {
        return;
    }

    // Read the current Rating value
    var rating = ratingAttribute.getValue();

    // Show Comments only for Rating 1 or 2
    if (rating === 1 || rating === 2) {
        commentsControl.setVisible(true);
        commentsControl.setDisabled(false);
    }
    else {
        commentsControl.setVisible(false);
    }
}


/*
Runs whenever Rating changes.

For Rating 1 or 2:
- Show and enable Comments

For blank, 3, 4 or 5:
- Clear Comments
- Hide Comments
*/
function showHideCommentsOnChange(executionContext) {

    // Get the current Feedback form
    var formContext = executionContext.getFormContext();

    // Get Rating
    var ratingAttribute =
        formContext.getAttribute("trpj_rating");

    // Get Comments data and control
    var commentsAttribute =
        formContext.getAttribute("trpj_comments");

    var commentsControl =
        formContext.getControl("trpj_comments");

    // Stop safely if fields are unavailable
    if (
        ratingAttribute === null ||
        commentsAttribute === null ||
        commentsControl === null
    ) {
        return;
    }

    // Read the selected Rating
    var rating = ratingAttribute.getValue();

    // Rating 1 or 2: show Comments and allow editing
    if (rating === 1 || rating === 2) {
        commentsControl.setVisible(true);
        commentsControl.setDisabled(false);
    }
    else {
        /*
         * Rating is blank, 3, 4 or 5.
         * Clear the previous Comments value before hiding the field.
         */
        commentsAttribute.setValue(null);

        // Ensure the cleared value is included when the form is saved
        commentsAttribute.setSubmitMode("always");

        // Hide Comments
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

    // Get the Submitted On control
    var submittedOnControl =
        formContext.getControl("trpj_submittedon");

    // Stop if Submitted On is unavailable
    if (submittedOnControl === null) {
        return;
    }

    // Form type 1 means Create
    if (formType === 1) {
        submittedOnControl.setDisabled(false);
    }
    else {
        submittedOnControl.setDisabled(true);
    }
}