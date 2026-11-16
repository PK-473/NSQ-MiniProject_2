# M4 (Jay) \- Feedback Validations, App Shell, Auditing, Plugins and Flow

**Project:** NSQ Event Management System (Microsoft Dynamics 365 / Dataverse)

**Owner:** Jay Kadam (M4)

**Branch:** feature/jay

**Publisher prefix:** trpj\_

**Primary table owned:** **Feedback** (trpj\_feedback)

**Additional ownership:** Model\-driven app shell, sitemap, auditing, shared plug\-in assembly support, testing, documentation, and R9 Power Automate flow

---

## 1\. Summary of What I Built

M4 owns the Feedback table validations and several shared application\-level responsibilities.

### Validation implementation summary

| Technique | Count | Requirements |
| --- | --- | --- |
| JavaScript | 2 | F1, F7 |
| Business Rule | 1 | F9 |
| Dataverse column configuration | 1 | F6 |
| C\# plug\-ins | 5 | F2, F4, F5, F8, F10 |
| Shared C\# plug\-in | 1 | S7/F3 |
| Power Automate | 1 | R9 |

### Other M4 work

- Feedback table, columns, form and views
- Model\-driven app shell
- Sitemap with Area, Groups and six table Sub\-areas
- Application access for System Administrator and System Customizer
- Auditing for Event, Session, Event Registration and Feedback
- Shared plug\-in assembly registration in Plugin Registration Tool
- Plug\-in tracing and profiling
- Shared Dataverse connection\-reference planning
- Validation tracking, regression testing and documentation

---

## 2\. Why Each Technology Was Used

| Technique | Used when | Reason |
| --- | --- | --- |
| JavaScript | Immediate form behavior is required | Runs in the browser and gives instant feedback without waiting for save |
| Business Rule | Simple conditional requirement on the same table | No\-code implementation and automatically reverses the required state |
| Dataverse column configuration | A standard data\-type or numeric limit is sufficient | Enforces the configured limit without duplicate custom code |
| C\# plug\-in | Validation must apply to form saves, imports, APIs and integrations | Runs server\-side and can block invalid create, update or delete operations |
| Power Automate | A related group of Dataverse records must be updated after a business event | Supports automated multi\-row processing and solution\-aware deployment |
| Environment Variable | Configuration may change between environments | Removes configuration such as forbidden words from the compiled plug\-in |
| Connection Reference | Solution flows need portable authenticated connector mapping | Allows each target environment to map the imported reference to a local connection |

---

## 3\. Validation Map

| ID | Validation | Technology | Implementation |
| --- | --- | --- | --- |
| F1 | Show Comments only when Rating is 1 or 2 | JavaScript | Feedback.js |
| F2 | Automatically populate Submitted On during Feedback creation | C\# plug\-in | SetFeedbackSubmittedOnPlugin |
| F4 | Prevent forbidden whole words in Comments | C\# plug\-in \+ Environment Variable | FeedbackProfanityValidationPlugin |
| F5 | Only attendees with an Attended registration can submit Feedback | C\# plug\-in | ValidateFeedbackAttendancePlugin |
| F6 | Rating must be between 1 and 5 | Dataverse column configuration | Rating minimum/maximum |
| F7 | Submitted On remains read\-only after creation | JavaScript | Feedback.js |
| F8 | Feedback can be submitted only for a Completed Session | C\# plug\-in | ValidateFeedbackSessionStatusPlugin |
| F9 | Comments mandatory when Rating is 1 or 2 | Business Rule | NSQ \- Feedback \- Comments Required |
| F10 | Prevent duplicate Feedback for the same Attendee and Session | C\# plug\-in | PreventDuplicateFeedbackPlugin |
| S7/F3 | Prevent Session deletion when related Feedback exists | Shared C\# plug\-in | PreventSessionDeletionWithFeedbackPlugin |
| R9 | Cancel Registered registrations when Event becomes Cancelled | Power Automate | NSQ \- Event Cancellation \- Cancel Active Registrations |

---

## 4\. Feedback Table

### Table details

| Property | Value |
| --- | --- |
| Display name | Feedback |
| Logical name | trpj\_feedback |
| Ownership | User or Team |
| Primary column | Feedback Number |
| Primary column type | Autonumber |
| Autonumber format | FB\-\{SEQNUM:6\} |

Example Feedback Numbers:

- FB\-000001
- FB\-000002
- FB\-000003

### Custom columns

| Display name | Logical name | Type | Configuration |
| --- | --- | --- | --- |
| Feedback Number | Primary name column | Autonumber | Business required, read\-only |
| Session | trpj\_session | Lookup to Session | Business required |
| Attendee | trpj\_attendee | Lookup to Attendee | Business required |
| Rating | trpj\_rating | Whole Number | Business required, minimum 1, maximum 5 |
| Comments | trpj\_comments | Multiple Lines of Text | Optional at column level |
| Submitted On | trpj\_submittedon | Date and Time | Automatically populated |

Comments is optional at the Dataverse column level because F9 controls the requirement level dynamically.

---

## 5\. Feedback Form and Views

### Main form

**Form name:** Feedback Main Form

### Form layout

**General tab**

**Feedback Details section**

- Feedback Number
- Session
- Attendee
- Rating

**Additional Details section**

- Comments
- Submitted On

### Form behavior

- Feedback Number is read\-only.
- Session and Attendee are Business Required.
- Rating accepts whole numbers from 1 through 5.
- Comments is shown only for Rating 1 or 2.
- Comments becomes required for Rating 1 or 2.
- Comments is cleared when Rating changes to blank, 3, 4 or 5.
- Submitted On becomes read\-only after record creation.
- No unnecessary Timeline or extra tabs are used on the Feedback form.

### Views

#### Active Feedback

Shows active Feedback records with the main business columns.

#### All Feedback

Shows both active and inactive Feedback records for administration and reporting.

---

## 6\. JavaScript: JavaScript/Feedback/Feedback.js

The Feedback JavaScript web resource is registered on the Feedback Main Form.

### Strict mode

The file starts with "use strict";.

Strict mode helps detect:

- Undeclared variables
- Accidental global variables
- Unsafe assignments
- JavaScript mistakes that would otherwise fail silently

This is important because the model\-driven form can load JavaScript libraries created by multiple team members.

### F1: Comments Visibility and Clearing

#### Required behavior

| Rating | Comments behavior |
| --- | --- |
| Blank | Clear and hide |
| 1 | Show and enable |
| 2 | Show and enable |
| 3 | Clear and hide |
| 4 | Clear and hide |
| 5 | Clear and hide |

### Functions

| Function | Event | Purpose |
| --- | --- | --- |
| showHideCommentsOnLoad | Form OnLoad | Shows or hides Comments without clearing saved data during form load |
| showHideCommentsOnChange | Rating OnChange | Shows Comments for Rating 1 or 2; otherwise clears and hides Comments |
| lockSubmittedOn | Form OnLoad | Locks Submitted On on existing Feedback records |

### Why OnLoad and OnChange use separate functions

The OnLoad function must not automatically delete existing data merely because a record is opened.

The OnChange function handles active user changes. When the user changes Rating from 1 or 2 to blank, 3, 4 or 5, it:

1. Clears Comments.
2. Uses setSubmitMode("always") so the cleared value is submitted.
3. Hides Comments.

This fixes the regression where old Comments reappeared after a Rating sequence such as 2 → 3 → 2.

### Event registration

| Form event | Function | Pass execution context |
| --- | --- | --- |
| Form OnLoad | showHideCommentsOnLoad | Yes |
| Form OnLoad | lockSubmittedOn | Yes |
| Rating OnChange | showHideCommentsOnChange | Yes |

---

## 7\. Business Rule: F9

### Rule name

NSQ \- Feedback \- Comments Required

### Scope

Entity/Table scope

### Logic

**IF**

Rating equals 1 OR Rating equals 2

**THEN**

Set Comments as Business Required

**ELSE**

Set Comments as Not Business Required

### Expected behavior

| Rating | Comments requirement |
| --- | --- |
| Blank | Optional |
| 1 | Required |
| 2 | Required |
| 3 | Optional |
| 4 | Optional |
| 5 | Optional |

The ELSE branch provides reverse or flip\-flop behavior. For example, changing Rating from 2 to 4 changes Comments from required to optional.

### Separation of responsibilities

| Behavior | Owner |
| --- | --- |
| Show, hide and clear Comments | F1 JavaScript |
| Make Comments required or optional | F9 Business Rule |

JavaScript does not change the Comments requirement level, preventing conflicts with the Business Rule.

---

## 8\. Dataverse Column Validation: F6

### Requirement

Rating must be between 1 and 5.

### Implementation

Rating is configured as:

- Data type: Whole Number
- Minimum value: 1
- Maximum value: 5
- Requirement level: Business Required

Dataverse displays a standard range\-validation message for values outside 1 through 5.

### Decimal behavior

Decimal values such as 2.5 are not supported because the Rating column is a Whole Number column.

Changing Rating to a Decimal Number would affect:

- F1 comparison logic
- F9 Business Rule conditions
- Rating control behavior
- Existing records
- Forms, views and tests

Therefore, Rating currently supports integer values 1, 2, 3, 4 and 5.

---

## 9\. Plug\-in Project and Shared Assembly

### Visual Studio solution

Plugins/NSQ.EventManagement.Plugins.sln

### Project

NSQ.EventManagement.Plugins

### Framework

.NET Framework 4.6.2

### Shared assembly

NSQ.EventManagement.Plugins.dll

### Signing key

NSQ.EventManagement.snk

### NuGet package

Microsoft.CrmSdk.CoreAssemblies

### Shared base class

All plug\-ins inherit from:

NSQ.EventManagement.Plugins.Common.PluginBase

The base class provides:

- IPlugin implementation
- Execution context
- Organization service
- Tracing service
- Shared exception handling

### One\-assembly rule

The team uses one shared assembly for all Event, Session, Speaker, Attendee, Event Registration and Feedback plug\-in classes.

New classes are added to the existing project. After changes are merged into dev, the complete solution is built in Release mode and the existing registered assembly is updated.

A separate assembly must not be registered for every plug\-in class.

---

## 10\. Feedback Plug\-ins

### F2: Set Submitted On

**Class:** SetFeedbackSubmittedOnPlugin

### Purpose

Automatically populates Submitted On with the current UTC date and time when Feedback is created.

### Registration

| Setting | Value |
| --- | --- |
| Message | Create |
| Primary table | trpj\_feedback |
| Stage | PreOperation |
| Mode | Synchronous |
| Execution order | 1 |
| Images | None |

### Why PreOperation

PreOperation allows the plug\-in to add Submitted On directly to the incoming Target before Dataverse saves the row. No additional update request is needed.

---

### F4: Forbidden Words Validation

**Class:** FeedbackProfanityValidationPlugin

### Purpose

Prevents Feedback Comments containing configured forbidden whole words.

### Rules

- Case\-insensitive matching
- Whole words only
- Blank Comments allowed
- Runs on Create and Update
- Does not trace the complete Comments value
- Does not hardcode the word list in C\#

### Environment Variable

| Property | Value |
| --- | --- |
| Display name | NSQ Forbidden Words |
| Schema name | trpj\_ForbiddenWords |
| Data type | Text |
| Value format | Comma, semicolon or new\-line separated words |

Example value:

idiot,stupid,damn,nonsense,useless

### Environment Variable resolution

1. Read the Current Value for the environment.
2. If Current Value is empty, use the Default Value.
3. Split the configured text into individual words.
4. Perform case\-insensitive whole\-word matching.

The word list can therefore change between Development, Test and Production without rebuilding the plug\-in assembly.

### Create step

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

### Update step

| Setting | Value |
| --- | --- |
| Message | Update |
| Table | trpj\_feedback |
| Filtering attribute | trpj\_comments |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

---

### F5: Validate Attendee Attendance

**Class:** ValidateFeedbackAttendancePlugin

### Purpose

Allows Feedback only when the selected Attendee has an Event Registration marked Attended for the Event associated with the selected Session.

### Relationship checked

Feedback Attendee

- Feedback Session → Session Event
- Event Registration Event
- Event Registration Attendee
- Registration Status = Attended

### Logical names and values

| Item | Value |
| --- | --- |
| Feedback Attendee lookup | trpj\_attendee |
| Feedback Session lookup | trpj\_session |
| Session Event lookup | trpj\_eventid |
| Event Registration table | trpj\_eventregistration |
| Registration Event lookup | trpj\_eventid |
| Registration Attendee lookup | trpj\_attendeeid |
| Registration Status | trpj\_registrationstatus |
| Attended value | 738000002 |

### Create step

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

### Update protection

Attendance must also be checked if an existing Feedback record’s Attendee or Session is changed.

Recommended Update registration:

| Setting | Value |
| --- | --- |
| Message | Update |
| Table | trpj\_feedback |
| Filtering attributes | trpj\_attendee,trpj\_session |
| Stage | PreValidation |
| Mode | Synchronous |
| Pre Image name | PreImage |
| Pre Image columns | trpj\_attendee,trpj\_session |

The Update Target contains only changed columns. The Pre Image supplies the unchanged Attendee or Session value.

---

### F8: Completed Session Validation

**Class:** ValidateFeedbackSessionStatusPlugin

### Purpose

Allows Feedback only when the selected Session has Session Status equal to Completed.

### Logical names and value

| Item | Value |
| --- | --- |
| Session table | trpj\_session |
| Session Status | trpj\_session\_status |
| Completed value | 738000002 |

### Registration

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

### Expected behavior

- Completed Session → Feedback allowed
- Draft, Scheduled, In Progress or Cancelled Session → Feedback blocked
- Missing Session Status → Feedback blocked

---

### F10: Duplicate Feedback Prevention

**Class:** PreventDuplicateFeedbackPlugin

### Purpose

Prevents the same Attendee from submitting more than one Feedback record for the same Session.

### Unique business combination

Attendee \+ Session

### Expected behavior

| Combination | Result |
| --- | --- |
| Same Attendee and same Session | Blocked |
| Same Attendee and different Session | Allowed |
| Different Attendee and same Session | Allowed |
| Different Attendee and different Session | Allowed |

### Registration

| Setting | Value |
| --- | --- |
| Message | Create |
| Table | trpj\_feedback |
| Stage | PreValidation |
| Mode | Synchronous |
| Images | None |

If Attendee and Session remain editable after creation, an Update step using filtering attributes and a Pre Image should also protect the combination against duplicates created through later edits.

---

### Shared S7/F3: Prevent Session Deletion

**Class:** PreventSessionDeletionWithFeedbackPlugin

### Purpose

Prevents deletion of a Session when one or more Feedback records reference the Session.

### Registration

| Setting | Value |
| --- | --- |
| Message | Delete |
| Primary table | trpj\_session |
| Stage | PreValidation |
| Mode | Synchronous |
| Execution order | 1 |
| Images | None |

### User\-facing message

This Session cannot be deleted because related Feedback records exist. Delete the related Feedback records first.

### Expected behavior

- Session with Feedback → deletion blocked
- Session with multiple Feedback records → deletion blocked
- Session with no Feedback → deletion allowed
- Related Feedback deleted first → Session deletion allowed, unless another validation blocks it

---

## 11\. Model\-Driven App and Sitemap

### App name

NSQ Event Management

### Sitemap structure

**Area: Event Management**

#### Group: Event Planning

- Events
- Sessions
- Speakers

#### Group: Attendee Management

- Attendees
- Event Registrations

#### Group: Engagement

- Feedback

All six project tables are available as sitemap Sub\-areas.

### App access

The app is assigned only to:

- System Administrator
- System Customizer

App access is controlled by role assignment, while table privileges continue to be controlled by the Dataverse security roles.

---

## 12\. Auditing

M4 owns auditing configuration and verification.

### Environment\-level auditing

Auditing is enabled in each project environment through:

Power Platform admin center → Environment → Settings → Audit and logs → Audit settings → Start Auditing

### Audited tables

- Event
- Session
- Event Registration
- Feedback

### Table\-level configuration

Each table has **Audit changes to its data** enabled.

### Column\-level configuration

All required custom columns remain enabled for auditing.

### Verification

Audit History was verified by changing values such as:

- Event Status
- Session Status
- Registration Status
- Feedback Rating or Comments

The audit entry must show:

- Table and record
- Changed column
- Previous value
- New value
- Changed By
- Changed On

---

## 13\. Power Automate R9

### Flow name

NSQ \- Event Cancellation \- Cancel Active Registrations

### Requirement

When Event Status becomes Cancelled, all related Event Registrations currently in Registered status must be updated to Cancelled.

### Trigger

| Setting | Value |
| --- | --- |
| Connector | Microsoft Dataverse |
| Trigger | When a row is added, modified or deleted |
| Change type | Modified |
| Table | Events |
| Scope | Organization |
| Select columns | Event Status logical name |
| Filter | Event Status equals Cancelled |

### Choice values

| Choice | Value |
| --- | --- |
| Event Status: Cancelled | 100000003 |
| Registration Status: Registered | 738000000 |
| Registration Status: Cancelled | 738000001 |

### Main processing

1. Event Status changes to Cancelled.
2. List Event Registration rows where: 
    - Event lookup equals the triggering Event ID.
    - Registration Status equals Registered.
3. Use the value array returned by List rows.
4. Apply to each matching registration.
5. Update the current Event Registration to Cancelled.

### OData filter structure

The Event Registration query uses:

- Dynamic Event ID from the trigger
- Registered Status choice value
- Dataverse lookup filter syntax using \_<lookup>\_value

No Event or Event Registration record GUID is hardcoded.

### Error handling

The flow uses:

- Main Processing Scope
- Error Handling Scope
- Terminate action

The Error Handling Scope runs when Main Processing fails or times out.

The Terminate action marks the complete flow run as Failed and provides a meaningful error code and message.

### Expected results

| Registration | Expected result |
| --- | --- |
| Registered registration for the cancelled Event | Changed to Cancelled |
| Attended registration for the cancelled Event | Unchanged |
| Already Cancelled registration | Unchanged |
| Registered registration for another Event | Unchanged |
| Event with no matching registrations | Flow succeeds with zero loop iterations |

A skipped Update action with the message “there are no items to repeat” means List rows returned zero matching registrations. This is valid behavior rather than a flow failure.

---

## 14\. Connection References

All solution\-aware flows should use the approved shared Dataverse connection reference.

### Purpose

A connection stores authenticated credentials.

A connection reference is a solution component that points a flow to an environment\-specific connection.

### Development environment

The shared reference points to an approved Dataverse connection in the development environment.

### Target environment

During managed\-solution import:

1. The connection\-reference component is imported.
2. Authentication credentials are not transported.
3. The importer maps the reference to a valid Dataverse connection in the target environment.
4. All flows using the shared reference use that target connection.

The connection reference should use a neutral project name rather than a developer’s personal name.

---

## 15\. Environment Variables

Environment variables store configuration that may change between environments.

### M4 environment variable

trpj\_ForbiddenWords

### Usage

The Feedback profanity plug\-in reads forbidden words from this variable.

### Deployment behavior

- Definition travels with the solution.
- Current Value can be configured for each environment.
- Plug\-in code does not need to be changed when the word list changes.

Environment variables should also be used for environment\-specific URLs, recipient addresses, feature switches and fixed configuration identifiers.

Dynamic row IDs should come from triggers or action outputs rather than from hardcoded GUIDs.

---

## 16\. Testing Completed

| Requirement | Main test | Result |
| --- | --- | --- |
| F1 | Rating 1/2 shows Comments; 3/4/5 hides and clears Comments | Passed |
| F2 | Submitted On automatically populated on create | Passed |
| F4 | Forbidden whole words blocked on Create and Update | Passed |
| F5 | Only Attended attendees allowed to submit Feedback | Passed |
| F6 | Values outside 1–5 rejected | Passed |
| F7 | Submitted On locked after creation | Passed |
| F8 | Non\-completed Session blocked | Passed |
| F9 | Comments required for Rating 1 or 2 and optional otherwise | Passed |
| F10 | Same Attendee and Session duplicate blocked | Passed |
| S7/F3 | Session with Feedback cannot be deleted | Passed |
| R9 | Registered registrations cancelled when Event is cancelled | Passed |
| Auditing | Status/value changes visible in Audit History | Passed |
| Sitemap | All six tables available in correct Groups | Passed |

### Required regression scenarios

- Rating 2 → 3 → 2: Comments remains cleared.
- Rating 1 → 4 → 1: Comments remains cleared.
- Save and reopen after clearing Comments.
- Change Feedback Attendee after save and revalidate attendance.
- Change Feedback Session after save and revalidate attendance and Session Status.
- Same Attendee and Session duplicate remains blocked.
- Valid Feedback still receives Submitted On.
- Forbidden Comments blocked regardless of uppercase or lowercase.
- Session deletion blocked only when related Feedback exists.
- Event cancellation updates only related Registered registrations.
- Registrations belonging to other Events remain unchanged.

---

## 17\. Known Limitations and Important Notes

1. **JavaScript is form\-side only.** Imports, grid edits and APIs can bypass JavaScript. Server\-side plug\-ins protect the important cross\-table rules.
2. **Rating is a Whole Number.** Decimal ratings such as 2.5 are not stored. Supporting decimals would require a different Dataverse column type and changes to F1 and F9.
3. **F5 and F10 require Update protection if lookups remain editable.** Create\-only steps are insufficient when Attendee or Session can be changed after Feedback is saved.
4. **Whole\-word profanity matching depends on configured values.** The environment variable must contain valid separators and non\-empty words.
5. **The Shared Plug\-in Assembly must be updated carefully.** The Release DLL must be built from the latest dev branch so it contains every team member’s classes.
6. **Registered plug\-in steps belong to the environment.** Updating an assembly must preserve all existing plug\-in types and steps.
7. **Connection\-reference authentication does not travel between environments.** The target importer must map the imported reference to a valid target connection.
8. **Power Automate run timing is not immediate in every case.** Dataverse\-triggered flows normally run quickly but should be verified through Run History.

---

## 18\. Git Workflow

### Branch strategy

main → dev → feature/jay

M4 work is completed in feature/jay and merged into dev through pull requests.

### Allowed commit types

| Type | Usage |
| --- | --- |
| feat | New functionality |
| fix | Defect correction |
| refactor | Internal improvement without intended behavior change |
| docs | Documentation update |

### M4 commit examples

- feat(feedback): show comments only for ratings below 3
- feat(feedback): lock submitted on after record creation
- feat(feedback): auto\-populate submitted on during creation
- feat(feedback): restrict feedback to completed sessions
- feat(feedback): block forbidden words in comments
- feat(feedback): prevent duplicate attendee session feedback
- feat(feedback): restrict feedback to attended attendees
- feat(session): prevent deletion when feedback exists
- fix(feedback): clear comments when rating no longer requires them
- refactor(feedback): load forbidden words from environment variable
- docs(readme): document M4 feedback implementation

### Important Git rules

- Do not use git add . in the shared plug\-in project.
- Stage only intended files.
- Review git status and git diff \-\-cached.
- Do not commit bin, obj, .vs, .dll, .pdb or .user files.
- The .csproj must include every C\# class exactly once.
- Preserve valid class entries from all branches when resolving .csproj conflicts.
- Pull the latest dev before starting new plug\-in work or updating the shared assembly.

---

## 19\. Deployment

### Source environment preparation

1. Merge approved work into dev.
2. Build the shared plug\-in project in Release.
3. Publish all Dataverse customizations.
4. Confirm all flows are inside the solution.
5. Confirm the shared Dataverse connection reference is included.
6. Confirm environment\-variable definitions are included.
7. Confirm plug\-in assembly and registered steps are included.
8. Confirm the model\-driven app and six tables are included.
9. Test JavaScript, Business Rules, plug\-ins, flows and auditing.
10. Export the solution as Managed for the target Test or Demo environment.

### Target environment import

1. Create or verify a valid target Dataverse connection.
2. Import the managed solution.
3. Map the imported connection reference to the target Dataverse connection.
4. Enter target environment\-variable values.
5. Complete the import.
6. Verify flow ownership and connection\-reference mapping.
7. Turn on imported flows if required.
8. Confirm plug\-in steps are enabled.
9. Open the model\-driven app.
10. Run full regression testing in the target environment.

### Preferred Solution

Setting the NSQ solution as the Preferred Solution helps new development components and customizations enter the correct solution automatically.

Preferred Solution does not:

- Transfer authentication credentials
- Remove the need to map connection references
- Replace managed\-solution import
- Automatically configure target environment\-variable values

---

## 20\. M4 Completion Summary

| Deliverable | Status |
| --- | --- |
| Feedback table | Completed |
| Feedback form | Completed |
| Feedback views | Completed |
| F1 JavaScript | Completed |
| F2 plug\-in | Completed |
| F4 plug\-in and Environment Variable | Completed |
| F5 plug\-in | Completed |
| F6 Dataverse validation | Completed |
| F7 JavaScript | Completed |
| F8 plug\-in | Completed |
| F9 Business Rule | Completed |
| F10 plug\-in | Completed |
| Shared Session deletion plug\-in | Completed |
| Model\-driven app shell | Completed |
| Sitemap | Completed |
| Security\-role visibility | Completed |
| Auditing | Completed |
| R9 Power Automate flow | Completed |
| Regression testing | Completed |
| Documentation | Completed |

